using Microsoft.AspNetCore.Mvc;

namespace MockApi2.XmlProvider.Controllers;

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
        _logger.LogDebug("API2: Health check requested");
        
        return Ok(new { 
            status = "healthy", 
            service = "MockApi2.XmlProvider",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}