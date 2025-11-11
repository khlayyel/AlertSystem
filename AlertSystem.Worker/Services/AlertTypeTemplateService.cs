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
            // For typeAlertId = 1 (Rupture)
            if (typeAlertId == 1)
            {
                var nomProduit = stockItem.TryGetProperty("Nom produit", out var np) 
                    ? np.GetString() ?? string.Empty 
                    : stockItem.TryGetProperty("Nom_produit", out var np2) 
                        ? np2.GetString() ?? string.Empty 
                        : string.Empty;

                var qte = stockItem.TryGetProperty("qte", out var q) && q.ValueKind == JsonValueKind.Number 
                    ? q.GetInt32() 
                    : 0;

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

            // Default template for other types
            var defaultNom = stockItem.TryGetProperty("Nom produit", out var dnp) 
                ? dnp.GetString() ?? string.Empty 
                : stockItem.TryGetProperty("Nom_produit", out var dnp2) 
                    ? dnp2.GetString() ?? string.Empty 
                    : string.Empty;

            return ($"Alerte {typeAlerteDescription}", defaultNom);
        }
    }
}

