using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using AlertSystem.Data;

namespace AlertSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/test")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly ApplicationDbContext _db;

        public TestController(ILogger<TestController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            _logger.LogInformation("Test ping endpoint called");
            return Ok(new { message = "Pong! API is working", timestamp = DateTime.UtcNow, environment = Environment.MachineName });
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            var health = new { status = "healthy", timestamp = DateTime.UtcNow, version = "1.0.0" };
            return Ok(health);
        }

        [HttpGet("database")]
        public async Task<IActionResult> Database()
        {
            try
            {
                var canConnect = await _db.Database.CanConnectAsync();
                if (!canConnect) return StatusCode(500, new { error = "Cannot connect to database" });

                var result = new
                {
                    status = "database_connected",
                    canConnect,
                    tableCounts = new
                    {
                        alertType = await _db.AlertType.CountAsync(),
                        statut = await _db.Statut.CountAsync(),
                        etat = await _db.Etat.CountAsync(),
                        apiClients = await _db.ApiClients.CountAsync()
                    },
                    timestamp = DateTime.UtcNow
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
