using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Tasks;
using AlertSystem.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.WatcherWorker.Services;

public interface IAppResolver
{
    Task<int?> ResolveAppIdAsync(ApplicationDbContext db, string domainCode, params string[] nameHints);
}

public sealed class AppResolver : IAppResolver
{
    private readonly ConcurrentDictionary<string, int?> _cache = new();

    public async Task<int?> ResolveAppIdAsync(ApplicationDbContext db, string domainCode, params string[] nameHints)
    {
        if (_cache.TryGetValue(domainCode, out var cached)) return cached;

        // Try code match then name hints
        var sql = @"SELECT TOP 1 ApplicationId
                    FROM def_application
                    WHERE application_code = @code
                    UNION ALL
                    SELECT TOP 1 ApplicationId
                    FROM def_application
                    WHERE " + string.Join(" OR ", nameHints.Select((_, i) => $"application_name LIKE @n{i}")) + ";";

        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        var pCode = cmd.CreateParameter();
        pCode.ParameterName = "@code";
        pCode.Value = domainCode;
        cmd.Parameters.Add(pCode);
        for (var i = 0; i < nameHints.Length; i++)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@n{i}";
            p.Value = "%" + nameHints[i] + "%";
            cmd.Parameters.Add(p);
        }

        if (cmd.Connection.State != ConnectionState.Open)
            await cmd.Connection.OpenAsync();

        var result = await cmd.ExecuteScalarAsync();
        var appId = result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
        _cache[domainCode] = appId;
        return appId;
    }
}


