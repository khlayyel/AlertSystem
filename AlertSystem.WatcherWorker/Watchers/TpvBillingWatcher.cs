using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AlertSystem.Data;
using AlertSystem.WatcherWorker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlertSystem.WatcherWorker.Watchers;

public sealed class TpvBillingWatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TpvBillingWatcher> _logger;
    private readonly ITimeProvider _time;
    private readonly IAppResolver _appResolver;
    private readonly ICapabilityResolver _capResolver;
    private readonly IRecipientResolver _recipients;
    private readonly IAlertWriter _writer;
    private readonly IConfiguration _config;

    public TpvBillingWatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<TpvBillingWatcher> logger,
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSec = _config.GetValue<int?>("WatcherIntervals:TPVSeconds") ?? 120;
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await TickAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "TpvBillingWatcher tick failed"); }
            await Task.Delay(TimeSpan.FromSeconds(intervalSec), stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = _time.UtcNow;

        // Aged TPV tickets example: tpv_facture without payment after N minutes since creation
        var minutesAged = _config.GetValue<int?>("WatcherThresholds:TPV:MinutesAgedTicket") ?? 45;
        var sql = @"SELECT tf.tpv_fact_id, tf.def_hotel AS hotel_id, tf.tpv_fact_date_creat
                    FROM tpv_facture tf
                    WHERE DATEADD(minute, @m, tf.tpv_fact_date_creat) <= @now";

        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@m"; p1.Value = minutesAged; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@now"; p2.Value = now; cmd.Parameters.Add(p2);
        if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync(ct);

        var items = new List<(int HotelId, DateTime Created)>();
        using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            var h = reader.GetOrdinal("hotel_id");
            var d = reader.GetOrdinal("tpv_fact_date_creat");
            while (await reader.ReadAsync(ct))
            {
                items.Add((Convert.ToInt32(reader.GetValue(h)), Convert.ToDateTime(reader.GetValue(d))));
            }
        }
        if (items.Count == 0) return;

        var appId = await _appResolver.ResolveAppIdAsync(db, "TPV", "tpv");
        var actions = _capResolver.GetActionsForDomain("TPV");

        foreach (var grp in items.GroupBy(x => x.HotelId))
        {
            var recipients = await _recipients.ResolveByHotelAndActionsAsync(db, grp.Key, actions);
            if (recipients.Count == 0) recipients = await _recipients.ResolveAdminsAsync(db, grp.Key);
            if (recipients.Count == 0) continue;
            var title = "[TPV] Tickets âgés";
            var body = $"{grp.Count()} tickets sans règlement depuis ≥ {minutesAged} min";
            await _writer.InsertAlertsAsync(db, appId, 1, title, body, 1, 1, recipients);
        }
    }
}


