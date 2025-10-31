using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.WatcherWorker.Services;

public sealed class Recipient
{
    public decimal? UserId { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

public interface IRecipientResolver
{
    Task<IReadOnlyList<Recipient>> ResolveByHotelAndActionsAsync(ApplicationDbContext db, int hotelId, IReadOnlyList<string> actionCodes);
    Task<IReadOnlyList<Recipient>> ResolveAdminsAsync(ApplicationDbContext db, int hotelId);
}

public sealed class RecipientResolver : IRecipientResolver
{
    public async Task<IReadOnlyList<Recipient>> ResolveByHotelAndActionsAsync(ApplicationDbContext db, int hotelId, IReadOnlyList<string> actionCodes)
    {
        if (actionCodes.Count == 0) return Array.Empty<Recipient>();

        var inList = string.Join(",", actionCodes.Select((_, i) => $"@a{i}"));
        var sql = $@"SELECT DISTINCT u.util_id, u.util_email, u.util_tel
                     FROM def_utilisateur u
                     JOIN auth_hotel ah ON ah.util_id = u.util_id AND ah.hotel_id = @hotel
                     JOIN securite_user_action sua ON sua.util_id = u.util_id
                     WHERE sua.action_code IN ({inList})";

        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        var pHotel = cmd.CreateParameter();
        pHotel.ParameterName = "@hotel";
        pHotel.Value = hotelId;
        cmd.Parameters.Add(pHotel);
        for (var i = 0; i < actionCodes.Count; i++)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@a{i}";
            p.Value = actionCodes[i];
            cmd.Parameters.Add(p);
        }

        if (cmd.Connection.State != ConnectionState.Open)
            await cmd.Connection.OpenAsync();

        var list = new List<Recipient>();
        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        while (await reader.ReadAsync())
        {
            list.Add(new Recipient
            {
                UserId = reader.IsDBNull(0) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(0)),
                Email = reader.IsDBNull(1) ? null : reader.GetString(1),
                Phone = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }
        return list;
    }

    public async Task<IReadOnlyList<Recipient>> ResolveAdminsAsync(ApplicationDbContext db, int hotelId)
    {
        var sql = @"SELECT DISTINCT u.util_id, u.util_email, u.util_tel
                    FROM def_utilisateur u
                    JOIN auth_hotel ah ON ah.util_id = u.util_id AND ah.hotel_id = @hotel
                    WHERE u.util_profile IN ('ADMIN','HOTEL_ADMIN')";

        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        var pHotel = cmd.CreateParameter();
        pHotel.ParameterName = "@hotel";
        pHotel.Value = hotelId;
        cmd.Parameters.Add(pHotel);

        if (cmd.Connection.State != ConnectionState.Open)
            await cmd.Connection.OpenAsync();

        var list = new List<Recipient>();
        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        while (await reader.ReadAsync())
        {
            list.Add(new Recipient
            {
                UserId = reader.IsDBNull(0) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(0)),
                Email = reader.IsDBNull(1) ? null : reader.GetString(1),
                Phone = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }
        return list;
    }
}


