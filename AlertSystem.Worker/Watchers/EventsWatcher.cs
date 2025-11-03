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

public sealed class EventsWatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventsWatcher> _logger;
    private readonly ITimeProvider _time;
    private readonly IAppResolver _appResolver;
    private readonly ICapabilityResolver _capResolver;
    private readonly IRecipientResolver _recipients;
    private readonly IAlertWriter _writer;
    private readonly IConfiguration _config;

    public EventsWatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<EventsWatcher> logger,
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
        var today = _time.UtcNow.Date;

        var sql = @"SELECT b.bnq_event_id, b.def_hotel AS hotel_id
                    FROM bnq_event b
                    WHERE CAST(b.event_date_debut AS date) = @d";

        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@d"; p1.Value = today; cmd.Parameters.Add(p1);
        if (cmd.Connection.State != System.Data.ConnectionState.Open) await cmd.Connection.OpenAsync(ct);

        var hotels = new List<int>();
        using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            var h = r.GetOrdinal("hotel_id");
            while (await r.ReadAsync(ct)) hotels.Add(Convert.ToInt32(r.GetValue(h)));
        }
        if (hotels.Count == 0) return;

        var appId = await _appResolver.ResolveAppIdAsync(db, "BNQ", "bnq", "banquet");
        var actions = _capResolver.GetActionsForDomain("BNQ");

        foreach (var grp in hotels.GroupBy(x => x))
        {
            var recipients = await _recipients.ResolveByHotelAndActionsAsync(db, grp.Key, actions);
            if (recipients.Count == 0) recipients = await _recipients.ResolveAdminsAsync(db, grp.Key);
            if (recipients.Count == 0) continue;
            var title = "[BNQ] Évènements du jour";
            var body = $"{grp.Count()} évènements prévus aujourd'hui";
            await _writer.InsertAlertsAsync(db, appId, 1, title, body, 1, 1, recipients);
        }
    }
}


