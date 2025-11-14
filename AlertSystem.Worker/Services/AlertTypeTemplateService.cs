using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Worker.Services
{
    public sealed class AlertTypeTemplateService : IAlertTypeTemplateService
    {
        private readonly ILogger<AlertTypeTemplateService> _logger;

        public AlertTypeTemplateService(ILogger<AlertTypeTemplateService> logger)
        {
            _logger = logger;
        }

        public (string Title, string Description) ResolveTemplate(int typeAlertId, string typeAlerteDescription, JsonElement stockItem)
        {
            // Extract common fields
            var nomProduit = stockItem.TryGetProperty("Nom produit", out var np) 
                ? np.GetString() ?? string.Empty 
                : stockItem.TryGetProperty("Nom_produit", out var np2) 
                    ? np2.GetString() ?? string.Empty 
                    : string.Empty;

            var qte = stockItem.TryGetProperty("qte", out var q) && q.ValueKind == JsonValueKind.Number 
                ? q.GetInt32() 
                : 0;

            // For typeAlertId = 1 (Rupture): quantity < qte_min
            if (typeAlertId == 1)
            {
                var qteMin = stockItem.TryGetProperty("qte_min", out var qm) && qm.ValueKind == JsonValueKind.Number 
                    ? qm.GetInt32() 
                    : 0;

                // Check if qte < qte_min (rupture condition)
                if (qte < qteMin)
                {
                    var title = "ALERTE RUPTURE DE STOCK!";
                    var description = $"Le produit {nomProduit} est en rupture de stock. Quantité actuelle: {qte}, Quantité minimale: {qteMin}";
                    _logger.LogInformation("Generated rupture alert for {Produit}: qte={Qte}, qte_min={QteMin}", nomProduit, qte, qteMin);
                    return (title, description);
                }
                else
                {
                    // No alert needed
                    _logger.LogDebug("No alert needed for {Produit}: qte={Qte} >= qte_min={QteMin}", nomProduit, qte, qteMin);
                    return (string.Empty, string.Empty);
                }
            }

            // For typeAlertId = 2 (Seuil Maximum Dépassé): quantity > qte_max
            if (typeAlertId == 2)
            {
                var qteMax = stockItem.TryGetProperty("qte_max", out var qmx) && qmx.ValueKind == JsonValueKind.Number 
                    ? qmx.GetInt32() 
                    : stockItem.TryGetProperty("qteMax", out var qmx2) && qmx2.ValueKind == JsonValueKind.Number
                        ? qmx2.GetInt32()
                        : 0;

                // Check if qte > qte_max (seuil max dépassé condition)
                if (qteMax > 0 && qte > qteMax)
                {
                    var title = "ALERTE SEUIL MAXIMUM DÉPASSÉ!";
                    var description = $"Le produit {nomProduit} a dépassé son seuil maximum. Quantité actuelle: {qte}, Quantité maximale: {qteMax}";
                    _logger.LogInformation("Generated seuil max alert for {Produit}: qte={Qte}, qte_max={QteMax}", nomProduit, qte, qteMax);
                    return (title, description);
                }
                else
                {
                    // No alert needed
                    _logger.LogDebug("No alert needed for {Produit}: qte={Qte} <= qte_max={QteMax}", nomProduit, qte, qteMax);
                    return (string.Empty, string.Empty);
                }
            }

            // For typeAlertId = 3 (Seuil minimum atteint): quantity == qte_min
            if (typeAlertId == 3)
            {
                var qteMin = stockItem.TryGetProperty("qte_min", out var qm) && qm.ValueKind == JsonValueKind.Number 
                    ? qm.GetInt32() 
                    : 0;

                // Check if qte == qte_min (seuil min atteint condition)
                if (qteMin > 0 && qte == qteMin)
                {
                    var title = "ALERTE SEUIL MINIMUM ATTEINT!";
                    var description = $"Le produit {nomProduit} a atteint son seuil minimum. Quantité actuelle: {qte}, Quantité minimale: {qteMin}";
                    _logger.LogInformation("Generated seuil min atteint alert for {Produit}: qte={Qte}, qte_min={QteMin}", nomProduit, qte, qteMin);
                    return (title, description);
                }
                else
                {
                    // No alert needed
                    _logger.LogDebug("No alert needed for {Produit}: qte={Qte} != qte_min={QteMin}", nomProduit, qte, qteMin);
                    return (string.Empty, string.Empty);
                }
            }

            // For typeAlertId = 4 (Seuil maximum atteint): quantity == qte_max
            if (typeAlertId == 4)
            {
                var qteMax = stockItem.TryGetProperty("qte_max", out var qmx) && qmx.ValueKind == JsonValueKind.Number 
                    ? qmx.GetInt32() 
                    : stockItem.TryGetProperty("qteMax", out var qmx2) && qmx2.ValueKind == JsonValueKind.Number
                        ? qmx2.GetInt32()
                        : 0;

                // Check if qte == qte_max (seuil max atteint condition)
                if (qteMax > 0 && qte == qteMax)
                {
                    var title = "ALERTE SEUIL MAXIMUM ATTEINT!";
                    var description = $"Le produit {nomProduit} a atteint son seuil maximum. Quantité actuelle: {qte}, Quantité maximale: {qteMax}";
                    _logger.LogInformation("Generated seuil max atteint alert for {Produit}: qte={Qte}, qte_max={QteMax}", nomProduit, qte, qteMax);
                    return (title, description);
                }
                else
                {
                    // No alert needed
                    _logger.LogDebug("No alert needed for {Produit}: qte={Qte} != qte_max={QteMax}", nomProduit, qte, qteMax);
                    return (string.Empty, string.Empty);
                }
            }

            // Default template for other types (should not happen for stock alerts)
            return ($"Alerte {typeAlerteDescription}", nomProduit);
        }
    }
}

