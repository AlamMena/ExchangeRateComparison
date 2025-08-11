using Microsoft.AspNetCore.Mvc;

namespace MockApi3.JsonProvider.Controllers;

[ApiController]
[Route("[controller]")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Health status information</returns>
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogDebug("API3: Health check requested");
        
        return Ok(new { 
            status = "healthy", 
            service = "MockApi3.JsonProvider",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}