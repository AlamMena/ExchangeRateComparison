using Microsoft.AspNetCore.Mvc;
using MockApi1.JsonProvider.DTOs;

namespace MockApi1.JsonProvider.Controllers;

[ApiController]
[Route("[controller]")]
[Tags("Exchange")]
public class ExchangeController : ControllerBase
{
    private readonly ILogger<ExchangeController> _logger;

    public ExchangeController(ILogger<ExchangeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get exchange rate for currency conversion
    /// </summary>
    /// <param name="request">Exchange request containing from/to currencies and amount</param>
    /// <param name="delay">Optional delay in milliseconds for testing</param>
    /// <param name="fail">Set to true to simulate failure</param>
    /// <returns>Exchange rate response</returns>
    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] ExchangeRequest request,
        [FromQuery] int? delay,
        [FromQuery] bool fail = false)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Endpoint"] = "/exchange"
        });

        _logger.LogInformation("API1: Processing exchange request - {From} to {To}, Amount: {Value}", 
            request.From, request.To, request.Value);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.From) || 
            string.IsNullOrWhiteSpace(request.To) || 
            request.Value <= 0)
        {
            _logger.LogWarning("API1: Invalid request - From: {From}, To: {To}, Value: {Value}", 
                request.From, request.To, request.Value);
            
            return BadRequest(new { 
                error = "Invalid request", 
                message = "From, To currencies must be provided and Value must be greater than 0" 
            });
        }

        // Check for unsupported currencies (simulate some API limitations)
        var unsupportedCurrencies = new[] { "XXX", "YYY", "ZZZ" };
        if (unsupportedCurrencies.Contains(request.From.ToUpperInvariant()) || 
            unsupportedCurrencies.Contains(request.To.ToUpperInvariant()))
        {
            _logger.LogWarning("API1: Unsupported currency - From: {From}, To: {To}", 
                request.From, request.To);
            
            return BadRequest(new { 
                error = "Unsupported currency", 
                message = $"Currency pair {request.From}/{request.To} is not supported" 
            });
        }

        // Simulate processing delay (configurable)
        var delayMs = delay ?? Random.Shared.Next(50, 200);
        
        if (delayMs > 0)
        {
            _logger.LogDebug("API1: Simulating {Delay}ms processing delay", delayMs);
            await Task.Delay(delayMs);
        }

        // Simulate random failures (if requested)
        if (fail)
        {
            _logger.LogWarning("API1: Simulating random failure");
            return StatusCode(503, new
            {
                error = "Service temporarily unavailable",
                message = "Simulated random failure for testing"
            });
        }

        // Calculate mock exchange rate
        var rate = CalculateExchangeRate(request.From, request.To);
        
        var response = new ExchangeResponse { Rate = rate };
        
        _logger.LogInformation("API1: Returning rate {Rate:F4} for {From}/{To} in {Delay}ms", 
            rate, request.From, request.To, delayMs);

        return Ok(response);
    }

    /// <summary>
    /// Get service information
    /// </summary>
    /// <returns>Service details and usage information</returns>
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        _logger.LogDebug("API1: Info endpoint requested");
        
        return Ok(new
        {
            service = "Mock API1 - JSON Exchange Rate Provider",
            version = "1.0.0",
            format = "JSON",
            port = 5001,
            endpoints = new
            {
                exchange = "POST /exchange - {from, to, value} → {rate}",
                health = "GET /health - Health check",
                info = "GET /exchange/info - Service information"
            },
            sampleRequest = new
            {
                method = "POST",
                url = "/exchange",
                body = new { from = "USD", to = "EUR", value = 100 }
            },
            sampleResponse = new
            {
                rate = 0.925m
            },
            queryParameters = new
            {
                delay = "Optional delay in milliseconds (e.g., ?delay=1000)",
                fail = "Set to 'true' to simulate failure (e.g., ?fail=true)"
            }
        });
    }

    /// <summary>
    /// Calculate mock exchange rate based on currency pair
    /// </summary>
    private static decimal CalculateExchangeRate(string from, string to)
    {
        // Generate deterministic but realistic rates based on currency pair
        var pair = $"{from.ToUpperInvariant()}{to.ToUpperInvariant()}";
        var hashCode = pair.GetHashCode();
        var seed = Math.Abs(hashCode % 1000);
        var random = new Random(seed);

        return from.ToUpperInvariant() switch
        {
            "USD" when to.ToUpperInvariant() == "EUR" => 0.85m + (decimal)(random.NextDouble() * 0.1),
            "USD" when to.ToUpperInvariant() == "GBP" => 0.75m + (decimal)(random.NextDouble() * 0.1),
            "USD" when to.ToUpperInvariant() == "DOP" => 55.5m + (decimal)(random.NextDouble() * 5.0),
            "EUR" when to.ToUpperInvariant() == "USD" => 1.15m + (decimal)(random.NextDouble() * 0.1),
            "EUR" when to.ToUpperInvariant() == "GBP" => 0.88m + (decimal)(random.NextDouble() * 0.1),
            "GBP" when to.ToUpperInvariant() == "USD" => 1.30m + (decimal)(random.NextDouble() * 0.1),
            "GBP" when to.ToUpperInvariant() == "EUR" => 1.12m + (decimal)(random.NextDouble() * 0.1),
            "DOP" when to.ToUpperInvariant() == "USD" => 0.018m + (decimal)(random.NextDouble() * 0.002),
            _ => 1.0m + (decimal)(random.NextDouble() * 0.2) // Default rate with variation
        };
    }
}