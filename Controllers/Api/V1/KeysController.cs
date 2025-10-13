using Microsoft.AspNetCore.Mvc;
using AlertSystem.Services;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public sealed class KeysController : ControllerBase
    {
        private readonly IApiKeyValidator _apiKeyValidator;
        private readonly ILogger<KeysController> _logger;

        public KeysController(IApiKeyValidator apiKeyValidator, ILogger<KeysController> logger)
        {
            _apiKeyValidator = apiKeyValidator;
            _logger = logger;
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateKey()
        {
            try
            {
                // Extract API key from header
                if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKeyValues))
                {
                    return Unauthorized(new { error = "Missing X-Api-Key header" });
                }

                var apiKey = apiKeyValues.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Unauthorized(new { error = "Empty X-Api-Key header" });
                }

                // Validate the API key
                var isValid = await _apiKeyValidator.ValidateApiKeyAsync(apiKey);
                
                if (isValid)
                {
                    _logger.LogInformation("API key validation successful for key ending with: ...{KeySuffix}", 
                        apiKey.Length > 6 ? apiKey[^6..] : "***");
                    
                    return Ok(new 
                    { 
                        valid = true, 
                        message = "API key is valid",
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning("API key validation failed for key ending with: ...{KeySuffix}", 
                        apiKey.Length > 6 ? apiKey[^6..] : "***");
                    
                    return Unauthorized(new 
                    { 
                        valid = false, 
                        error = "Invalid API key",
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestKey([FromBody] TestKeyDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ApiKey))
                {
                    return BadRequest(new { error = "API key is required" });
                }

                // Validate the provided API key
                var isValid = await _apiKeyValidator.ValidateApiKeyAsync(dto.ApiKey);
                
                if (isValid)
                {
                    _logger.LogInformation("API key test successful for key ending with: ...{KeySuffix}", 
                        dto.ApiKey.Length > 6 ? dto.ApiKey[^6..] : "***");
                    
                    return Ok(new 
                    { 
                        valid = true, 
                        message = "API key is valid and active",
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning("API key test failed for key ending with: ...{KeySuffix}", 
                        dto.ApiKey.Length > 6 ? dto.ApiKey[^6..] : "***");
                    
                    return Ok(new 
                    { 
                        valid = false, 
                        message = "API key is invalid or inactive",
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API key");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        public sealed class TestKeyDto
        {
            public string ApiKey { get; set; } = string.Empty;
        }
    }
}
