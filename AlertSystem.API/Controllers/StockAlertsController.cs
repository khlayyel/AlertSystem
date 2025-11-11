using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/v1/stock-alerts")]
    public sealed class StockAlertsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var baseDir = AppContext.BaseDirectory;
            var filePath = Path.Combine(baseDir, "App_Data", "alerts.json");
            if (!System.IO.File.Exists(filePath))
            {
                var sample = new
                {
                    stock_alerts = new object[]
                    {
                        new { Nom_produit = "boissons fanta", qte_min = 10, qte_max = 20, qte = 3 },
                        new { Nom_produit = "tomates", qte_min = 5, qte_max = 40, qte = 1 }
                    }
                };
                return Ok(sample);
            }
            var json = System.IO.File.ReadAllText(filePath);
            try
            {
                var doc = JsonDocument.Parse(json);
                return Content(json, "application/json");
            }
            catch
            {
                return BadRequest(new { error = "Invalid alerts.json content" });
            }
        }
    }
}

