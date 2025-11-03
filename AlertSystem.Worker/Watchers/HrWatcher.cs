using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlertSystem.Data;
using AlertSystem.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Worker.Watchers;

public sealed class HrWatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HrWatcher> _logger;
    private readonly ITimeProvider _time;
    private readonly IAppResolver _appResolver;
    private readonly ICapabilityResolver _capResolver;
    private readonly IRecipientResolver _recipients;
    private readonly IAlertWriter _writer;
    private readonly IConfiguration _config;

    public HrWatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<HrWatcher> logger,
        ITimeProvider time,
        IAppResolver appResolver,
        ICapabilityResolver capResolver,
        IRecipientResolver recipients,
        IAlertWriter writer,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _time = time;
        _appResolver = appResolver;
        _capResolver = capResolver;
        _recipients = recipients;
        _writer = writer;
        _config = config;
    }

    public async Task ExecuteTickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = _time.UtcNow;

        var days = _config.GetValue<int?>("WatcherThresholds:GRH:ContractEndDays") ?? 30;
        var sql = @"SELECT c.grh_emp_contrat_id, c.def_hotel AS hotel_id, c.grh_emp_contrat_date_fin
                    FROM grh_employe_contrat c
                    WHERE c.grh_emp_contrat_date_fin IS NOT NULL
                      AND c.grh_emp_contrat_date_fin <= DATEADD(day, @d, @now)";

        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@d"; p1.Value = days; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@now"; p2.Value = now; cmd.Parameters.Add(p2);
        if (cmd.Connection.State != System.Data.ConnectionState.Open) await cmd.Connection.OpenAsync(ct);

        var items = new List<(int HotelId, DateTime End)>();
        using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            var h = r.GetOrdinal("hotel_id");
            var d = r.GetOrdinal("grh_emp_contrat_date_fin");
            while (await r.ReadAsync(ct)) items.Add((Convert.ToInt32(r.GetValue(h)), Convert.ToDateTime(r.GetValue(d))));
        }
        if (items.Count == 0) return;

        var appId = await _appResolver.ResolveAppIdAsync(db, "GRH", "grh", "ressources humaines");
        var actions = _capResolver.GetActionsForDomain("GRH");
        foreach (var grp in items.GroupBy(x => x.HotelId))
        {
            var recipients = await _recipients.ResolveByHotelAndActionsAsync(db, grp.Key, actions);
            if (recipients.Count == 0) recipients = await _recipients.ResolveAdminsAsync(db, grp.Key);
            if (recipients.Count == 0) continue;
            var title = "[GRH] Contrats arrivant à échéance";
            var body = $"{grp.Count()} contrats se terminent dans ≤ {days} jours";
            await _writer.InsertAlertsAsync(db, appId, 1, title, body, 1, 1, recipients);
        }
    }
}


