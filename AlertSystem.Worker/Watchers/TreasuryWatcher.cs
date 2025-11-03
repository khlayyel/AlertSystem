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

public sealed class TreasuryWatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TreasuryWatcher> _logger;
    private readonly IAppResolver _appResolver;
    private readonly ICapabilityResolver _capResolver;
    private readonly IRecipientResolver _recipients;
    private readonly IAlertWriter _writer;
    private readonly IConfiguration _config;

    public TreasuryWatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<TreasuryWatcher> logger,
        IAppResolver appResolver,
        ICapabilityResolver capResolver,
        IRecipientResolver recipients,
        IAlertWriter writer,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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

        var sql = @"SELECT t.def_hotel AS hotel_id
                    FROM tres_hotel_compte_banque t
                    WHERE (t.tres_hotel_cpte_b_solde_min IS NOT NULL AND t.tres_hotel_cpte_b_solde < t.tres_hotel_cpte_b_solde_min)
                       OR (t.tres_hotel_cpte_b_solde_max IS NOT NULL AND t.tres_hotel_cpte_b_solde > t.tres_hotel_cpte_b_solde_max)";

        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        if (cmd.Connection.State != System.Data.ConnectionState.Open) await cmd.Connection.OpenAsync(ct);

        var hotels = new List<int>();
        using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            var h = r.GetOrdinal("hotel_id");
            while (await r.ReadAsync(ct)) hotels.Add(Convert.ToInt32(r.GetValue(h)));
        }
        if (hotels.Count == 0) return;

        var appId = await _appResolver.ResolveAppIdAsync(db, "TRES", "tres", "trésor", "tresorerie");
        var actions = _capResolver.GetActionsForDomain("TRES");
        foreach (var grp in hotels.GroupBy(x => x))
        {
            var recipients = await _recipients.ResolveByHotelAndActionsAsync(db, grp.Key, actions);
            if (recipients.Count == 0) recipients = await _recipients.ResolveAdminsAsync(db, grp.Key);
            if (recipients.Count == 0) continue;
            var title = "[TRES] Seuils de trésorerie franchis";
            var body = "Compte(s) en dehors des seuils min/max";
            await _writer.InsertAlertsAsync(db, appId, 1, title, body, 1, 1, recipients);
        }
    }
}


