using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AlertSystem.Worker.Services;

namespace AlertSystem.Worker.Watchers
{
    public sealed class AlertPoller : IHotelDomainPoller
    {
        private readonly ILogger<AlertPoller> _logger;
        private readonly IConfiguration _cfg;
        private readonly IHotelApiClient _http;
        private readonly IAlertInsertService _inserter;
        private readonly IDefAlerteService _defAlerteService;
        private readonly IUserRecipientService _userRecipientService;
        private readonly IAlertTypeTemplateService _templateService;

        public AlertPoller(
            ILogger<AlertPoller> logger,
            IConfiguration cfg,
            IHotelApiClient http,
            IAlertInsertService inserter,
            IDefAlerteService defAlerteService,
            IUserRecipientService userRecipientService,
            IAlertTypeTemplateService templateService)
        {
            _logger = logger;
            _cfg = cfg;
            _http = http;
            _inserter = inserter;
            _defAlerteService = defAlerteService;
            _userRecipientService = userRecipientService;
            _templateService = templateService;
        }

        public int IntervalSeconds => _cfg.GetValue<int?>("Polling:Alerts:IntervalSeconds") ?? 300;
        public bool Enabled => _cfg.GetValue<bool?>("Polling:Alerts:Enabled") ?? false;

        public async Task PollAsync(CancellationToken ct)
        {
            if (!Enabled)
            {
                _logger.LogDebug("AlertPoller is disabled");
                return;
            }

            try
            {
                // Get all active def_Alerte
                var defAlertes = await _defAlerteService.GetActiveAlertesAsync(ct);
                _logger.LogInformation("Polling {Count} active def_alerte records", defAlertes.Count);

                foreach (var defAlerte in defAlertes)
                {
                    await ProcessDefAlerteAsync(defAlerte, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AlertPoller.PollAsync");
            }
        }

        private async Task ProcessDefAlerteAsync(DefAlerteModel defAlerte, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Processing def_alerte {DefAlerteId} from URL {URL}", defAlerte.DefAlerteId, defAlerte.URL);

                // Fetch JSON from URL
                var body = await _http.GetAsync(defAlerte.URL, ct);
                using var doc = JsonDocument.Parse(body);

                // Parse stock_alerts array
                if (!doc.RootElement.TryGetProperty("stock_alerts", out var stockAlertsEl) || stockAlertsEl.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("No stock_alerts array found in response from {URL}", defAlerte.URL);
                    return;
                }

                // Deduplication set: (AppId, TypeAlerteId, Nom produit, qte, destinataireId, platformId)
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var stockItem in stockAlertsEl.EnumerateArray())
                {
                    // Resolve template to get title and description
                    var (title, description) = _templateService.ResolveTemplate(
                        defAlerte.DefTypeAlerte,
                        defAlerte.TypeAlerteDescription,
                        stockItem);

                    // Skip if template returns empty (no alert needed)
                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
                    {
                        continue;
                    }

                    // Get product name for deduplication
                    var nomProduit = stockItem.TryGetProperty("Nom produit", out var np)
                        ? np.GetString() ?? string.Empty
                        : stockItem.TryGetProperty("Nom_produit", out var np2)
                            ? np2.GetString() ?? string.Empty
                            : string.Empty;

                    var qte = stockItem.TryGetProperty("qte", out var q) && q.ValueKind == JsonValueKind.Number
                        ? q.GetInt32()
                        : 0;

                    // Process each destinataire
                    foreach (var destinataireId in defAlerte.DestinatairesId)
                    {
                        // Get recipients (email and/or WhatsApp) for this user
                        var recipients = await _userRecipientService.GetRecipientsForUserAsync(destinataireId, ct);

                        foreach (var (platformId, recipient) in recipients)
                        {
                            // Deduplication key
                            var dedupKey = $"{defAlerte.AppId}|{defAlerte.DefTypeAlerte}|{nomProduit}|{qte}|{destinataireId}|{platformId}";
                            if (!seen.Add(dedupKey))
                            {
                                _logger.LogDebug("Skipping duplicate alert: {Key}", dedupKey);
                                continue;
                            }

                            // Determine TypeEnvoieId based on def_TypeAlerte or default to 2 (Obligatoire)
                            // For now, we'll use 2 (Obligatoire) for rupture alerts
                            var typeEnvoieId = 2; // Obligatoire

                            // Create alert intent
                            var intent = new AlertIntent
                            {
                                AppId = defAlerte.AppId,
                                TypeEnvoieId = typeEnvoieId,
                                Title = title,
                                Description = description,
                                Deliveries = new[] { (platformId, recipient) }
                            };

                            // Insert alert
                            await _inserter.InsertAsync(intent, ct);
                            _logger.LogInformation("Inserted alert for user {UserId}, platform {PlatformId}, product {Produit}",
                                destinataireId, platformId, nomProduit);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing def_alerte {DefAlerteId}", defAlerte.DefAlerteId);
            }
        }
    }
}
