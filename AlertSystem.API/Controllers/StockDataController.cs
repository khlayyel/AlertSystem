using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlertSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class StockDataController : ControllerBase
    {
        private readonly ILogger<StockDataController> _logger;

        public StockDataController(IConfiguration configuration, ILogger<StockDataController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            // Mock data only for development; hotels will implement an equivalent endpoint.
            var rawItems = new List<(string code, string nomProduit, int qt, int min, int max)>
            {
                ("A1", "Article A1", 2, 5, 15),  // EPUISE
                ("A2", "Article A2", 6, 5, 10),  // OK (ignored)
                ("A3", "Article A3", 12, 3, 10)  // MAX
            };

            // Shape into the worker contract: { domaineId, alertes: [ { nomProduit, etat, alertTypeId, destinataires: [emails] } ] }
            var alertes = new List<object>();
            foreach (var it in rawItems)
            {
                string? etat = null;
                if (it.qt < it.min) etat = "EPUISE";
                else if (it.qt > it.max) etat = "MAX";
                if (etat == null) continue; // only create alerts when out of bounds

                // Fixed recipients for test as requested
                var destinataires = new[] { "khalilouerghemmi@gmail.com", "zied.soltani11@gmail.com", "+21699414008" };

                var alertTypeId = etat == "EPUISE" ? 2 : 1; // 1=Information, 2=Obligatoire

                alertes.Add(new
                {
                    nomProduit = it.nomProduit,
                    code = it.code,
                    qt = it.qt,
                    min = it.min,
                    max = it.max,
                    etat,
                    alertTypeId,
                    destinataires
                });
            }

            var payload = new { domaineId = 1, alertes };
            return Ok(payload);
        }
    }
}


