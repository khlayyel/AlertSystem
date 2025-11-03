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

public sealed class ReservationsWatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationsWatcher> _logger;
    private readonly ITimeProvider _time;
    private readonly IAppResolver _appResolver;
    private readonly ICapabilityResolver _capResolver;
    private readonly IRecipientResolver _recipients;
    private readonly IAlertWriter _writer;
    private readonly IConfiguration _config;

    public ReservationsWatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationsWatcher> logger,
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

        var hoursBeforeCheckin = _config.GetValue<int?>("WatcherThresholds:RESA:HoursBeforeCheckin") ?? 3;
        var now = _time.UtcNow;

        var sql = @"SELECT TOP 50 r.rese_id, r.def_hotel AS hotel_id, r.resa_date_checkin, r.resa_heure_checkin
                    FROM resa r
                    WHERE DATEADD(hour, -@h, r.resa_date_checkin) <= @now AND r.resa_date_checkin >= @now";

        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@h"; p1.Value = hoursBeforeCheckin; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@now"; p2.Value = now; cmd.Parameters.Add(p2);

        if (cmd.Connection.State != System.Data.ConnectionState.Open) await cmd.Connection.OpenAsync(ct);

        var rows = new List<(int HotelId, DateTime CheckinDate)>();
        using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            var hotelOrdinal = reader.GetOrdinal("hotel_id");
            var dateOrdinal = reader.GetOrdinal("resa_date_checkin");
            while (await reader.ReadAsync(ct))
            {
                var hotelId = Convert.ToInt32(reader.GetValue(hotelOrdinal));
                var d = Convert.ToDateTime(reader.GetValue(dateOrdinal));
                rows.Add((hotelId, d));
            }
        }

        if (rows.Count == 0) return;

        var appId = await _appResolver.ResolveAppIdAsync(db, "RESA", "resa", "reservation");
        var actions = _capResolver.GetActionsForDomain("RESA");

        foreach (var grp in rows.GroupBy(x => x.HotelId))
        {
            var recipients = await _recipients.ResolveByHotelAndActionsAsync(db, grp.Key, actions);
            if (recipients.Count == 0)
            {
                recipients = await _recipients.ResolveAdminsAsync(db, grp.Key);
            }
            if (recipients.Count == 0) continue;

            var title = "[RESA] Arrivées imminentes";
            var body = $"{grp.Count()} arrivées dans ~{hoursBeforeCheckin}h";

            await _writer.InsertAlertsAsync(db,
                appId,
                alertTypeId: 1,
                title: title,
                description: body,
                statutPendingId: 1,
                plateformeId: 1,
                recipients: recipients);
        }
    }
}


