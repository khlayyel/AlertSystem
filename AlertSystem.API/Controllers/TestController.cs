using Microsoft.AspNetCore.Mvc;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
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
            try
            {
                _logger.LogInformation("Test ping endpoint called");
                return Ok(new { 
                    message = "Pong! API is working", 
                    timestamp = DateTime.UtcNow,
                    environment = Environment.MachineName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test ping endpoint");
                return StatusCode(500, new { error = "Test ping failed", details = ex.Message });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            try
            {
                _logger.LogInformation("Health check endpoint called");
                
                var health = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    uptime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    checks = new
                    {
                        api = "ok",
                        logging = "ok"
                    }
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in health check endpoint");
                return StatusCode(500, new { error = "Health check failed", details = ex.Message });
            }
        }

        [HttpGet("database")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                _logger.LogInformation("Database test endpoint called");
                
                // Test basic database connectivity
                var canConnect = await _db.Database.CanConnectAsync();
                _logger.LogInformation("Database CanConnect: {CanConnect}", canConnect);

                if (!canConnect)
                {
                    return StatusCode(500, new { error = "Cannot connect to database" });
                }

                // Test table counts
                var alertTypeCount = await _db.AlertType.CountAsync();
                var expedTypeCount = await _db.ExpedType.CountAsync();
                var statutCount = await _db.Statut.CountAsync();
                var etatCount = await _db.Etat.CountAsync();
                var apiClientCount = await _db.ApiClients.CountAsync();

                _logger.LogInformation("Table counts - AlertType: {AlertType}, ExpedType: {ExpedType}, Statut: {Statut}, Etat: {Etat}, ApiClients: {ApiClients}",
                    alertTypeCount, expedTypeCount, statutCount, etatCount, apiClientCount);

                var result = new
                {
                    status = "database_connected",
                    canConnect = canConnect,
                    tableCounts = new
                    {
                        alertType = alertTypeCount,
                        expedType = expedTypeCount,
                        statut = statutCount,
                        etat = etatCount,
                        apiClients = apiClientCount
                    },
                    timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in database test endpoint");
                return StatusCode(500, new { 
                    error = "Database test failed", 
                    details = ex.Message,
                    stackTrace = ex.StackTrace?.Split('\n').Take(5).ToArray()
                });
            }
        }
    }
}
