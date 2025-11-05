using Microsoft.AspNetCore.Mvc;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("mock")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class MockHotelDataController : ControllerBase
    {
        [HttpGet("stock")]
        public IActionResult GetStock()
        {
            var json = new
            {
                domaineId = 1,
                alertes = new[]
                {
                    new { nomProduit = "Coca-Cola", etat = "epuise", alertType = "STOCK_LOW", destinataires = new [] { "manager@hotel.com", "+21612345678" } },
                    new { nomProduit = "Fanta", etat = "max", alertType = "STOCK_HIGH", destinataires = new [] { "stock@hotel.com" } }
                }
            };
            return Ok(json);
        }
    }
}


