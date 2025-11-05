using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AlertSystem.Worker.Services;

namespace AlertSystem.Worker.Watchers
{
    public sealed class StockPoller : IHotelDomainPoller
    {
        private readonly ILogger<StockPoller> _logger;
        private readonly IConfiguration _cfg;
        private readonly IHotelApiClient _http;
        private readonly IAlertTemplateService _templates;
        private readonly IAlertInsertService _inserter;

        public StockPoller(ILogger<StockPoller> logger, IConfiguration cfg, IHotelApiClient http, IAlertTemplateService templates, IAlertInsertService inserter)
        { _logger = logger; _cfg = cfg; _http = http; _templates = templates; _inserter = inserter; }

        public int IntervalSeconds => _cfg.GetValue<int?>("Polling:Stock:IntervalSeconds") ?? 300;
        public bool Enabled => _cfg.GetValue<bool?>("Polling:Stock:Enabled") ?? false;

        public async Task PollAsync(CancellationToken ct)
        {
            if (!Enabled) return;
            var endpoint = _cfg["Polling:Stock:Endpoint"];
            if (string.IsNullOrWhiteSpace(endpoint)) return;

            var body = await _http.GetAsync(endpoint, ct);
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("domaineId", out var domaineEl) || domaineEl.GetInt32() != 1) return;
            if (!doc.RootElement.TryGetProperty("alertes", out var listEl) || listEl.ValueKind != JsonValueKind.Array) return;

            // Simple in-poll deduplication: code+etat+recipient
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in listEl.EnumerateArray())
            {
                var etat = item.TryGetProperty("etat", out var e) ? e.GetString() ?? string.Empty : string.Empty;
                var (title, description) = _templates.Resolve(1, etat, item);
                var recips = item.TryGetProperty("destinataires", out var d) && d.ValueKind == JsonValueKind.Array
                    ? d.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                    : new List<string>();

                var deliveries = recips.Select(r => (r.Contains('@') ? 1 : 2, r)).ToList();
                if (deliveries.Count == 0) continue;

                var code = item.TryGetProperty("code", out var c) ? c.GetString() ?? string.Empty : string.Empty;
                deliveries = deliveries
                    .Where(dv => seen.Add($"{code}|{etat}|{dv.Item1}|{dv.Item2}"))
                    .ToList();
                if (deliveries.Count == 0) continue;

                var intent = new AlertIntent
                {
                    DomaineId = 1,
                    TypeId = item.TryGetProperty("alertTypeId", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt32() : 1,
                    Title = title,
                    Description = description,
                    Deliveries = deliveries
                };
                await _inserter.InsertAsync(intent, ct);
            }
        }
    }
}


