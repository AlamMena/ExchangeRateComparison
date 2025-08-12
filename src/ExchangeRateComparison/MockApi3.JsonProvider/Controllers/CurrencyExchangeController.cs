using Microsoft.AspNetCore.Mvc;
using MockApi3.JsonProvider.DTOs;

namespace MockApi3.JsonProvider.Controllers;

[ApiController]
[Route("currency-exchange")]
[Tags("Exchange")]
public class CurrencyExchangeController(ILogger<CurrencyExchangeController> logger) : ControllerBase
{
    /// <summary>
    /// Exchange currency using nested JSON format
    /// </summary>
    /// <param name="request">Exchange request with a nested exchange object</param>
    /// <param name="delay">Optional delay in milliseconds for testing</param>
    /// <param name="fail">Set to true to simulate failure</param>
    /// <returns>Response with status code, message, and data containing total</returns>
    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] CurrencyExchangeRequest request,
        [FromQuery] int? delay,
        [FromQuery] bool fail = false)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Endpoint"] = "/currency-exchange"
        });

        logger.LogInformation("API3: Processing currency exchange request - {SourceCurrency} to {TargetCurrency}, Quantity: {Quantity}", 
            request.Exchange?.SourceCurrency, request.Exchange?.TargetCurrency, request.Exchange?.Quantity);

        // Validate request
        if (request.Exchange == null)
        {
            logger.LogWarning("API3: Missing exchange object in request");
            
            return Ok(new CurrencyExchangeResponse
            {
                StatusCode = 400,
                Message = "Missing exchange object in request",
                Data = null
            });
        }

        if (string.IsNullOrWhiteSpace(request.Exchange.SourceCurrency) || 
            string.IsNullOrWhiteSpace(request.Exchange.TargetCurrency) || 
            request.Exchange.Quantity <= 0)
        {
            logger.LogWarning("API3: Invalid exchange values - SourceCurrency: {SourceCurrency}, TargetCurrency: {TargetCurrency}, Quantity: {Quantity}", 
                request.Exchange.SourceCurrency, request.Exchange.TargetCurrency, request.Exchange.Quantity);
            
            return Ok(new CurrencyExchangeResponse
            {
                StatusCode = 400,
                Message = "SourceCurrency, TargetCurrency must be provided and Quantity must be greater than 0",
                Data = null
            });
        }

        // Check for unsupported currencies (simulate some API limitations)
        var unsupportedCurrencies = new[] { "XXX", "YYY", "ZZZ" };
        if (unsupportedCurrencies.Contains(request.Exchange.SourceCurrency.ToUpperInvariant()) || 
            unsupportedCurrencies.Contains(request.Exchange.TargetCurrency.ToUpperInvariant()))
        {
            logger.LogWarning("API3: Unsupported currency - SourceCurrency: {SourceCurrency}, TargetCurrency: {TargetCurrency}", 
                request.Exchange.SourceCurrency, request.Exchange.TargetCurrency);
            
            return Ok(new CurrencyExchangeResponse
            {
                StatusCode = 400,
                Message = $"Currency pair {request.Exchange.SourceCurrency}/{request.Exchange.TargetCurrency} is not supported",
                Data = null
            });
        }

        // Simulate processing delay (configurable)
        var delayMs = delay ?? Random.Shared.Next(100, 400);
        
        if (delayMs > 0)
        {
            logger.LogDebug("API3: Simulating {Delay}ms processing delay", delayMs);
            await Task.Delay(delayMs);
        }

        // Simulate random failures (if requested)
        if (fail)
        {
            logger.LogWarning("API3: Simulating random failure");
            
            return Ok(new CurrencyExchangeResponse
            {
                StatusCode = 503,
                Message = "Service temporarily unavailable - simulated failure for testing",
                Data = null
            });
        }

        // Calculate mock exchange rate and total
        var rate = CalculateExchangeRate(request.Exchange.SourceCurrency, request.Exchange.TargetCurrency);
        var total = request.Exchange.Quantity * rate;
        
        var response = new CurrencyExchangeResponse
        {
            StatusCode = 200,
            Message = "success",
            Data = new CurrencyExchangeData { Total = total }
        };
        
        logger.LogInformation("API3: Returning total {Total:F2} (rate {Rate:F4}) for {SourceCurrency}/{TargetCurrency} in {Delay}ms", 
            total, rate, request.Exchange.SourceCurrency, request.Exchange.TargetCurrency, delayMs);

        return Ok(response);
    }

    /// <summary>
    /// Get service information
    /// </summary>
    /// <returns>Service details and usage information</returns>
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        logger.LogDebug("API3: Info endpoint requested");
        
        return Ok(new
        {
            service = "Mock API3 - JSON Nested Exchange Rate Provider",
            version = "1.0.0",
            format = "JSON (Nested)",
            port = 5003,
            endpoints = new
            {
                exchange = "POST /currency-exchange - {exchange: {sourceCurrency, targetCurrency, quantity}} → {statusCode, message, data: {total}}",
                health = "GET /health - Health check",
                info = "GET /currency-exchange/info - Service information"
            },
            sampleRequest = new
            {
                method = "POST",
                url = "/currency-exchange",
                body = new { 
                    exchange = new { 
                        sourceCurrency = "USD", 
                        targetCurrency = "EUR", 
                        quantity = 100 
                    } 
                }
            },
            sampleResponse = new
            {
                statusCode = 200,
                message = "success",
                data = new { total = 91.8m }
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
    private static decimal CalculateExchangeRate(string sourceCurrency, string targetCurrency)
    {
        // Generate deterministic but realistic rates based on currency pair
        var pair = $"{sourceCurrency.ToUpperInvariant()}{targetCurrency.ToUpperInvariant()}";
        var hashCode = pair.GetHashCode();
        var seed = Math.Abs(hashCode % 1000);
        var random = new Random(seed);

        return sourceCurrency.ToUpperInvariant() switch
        {
            "USD" when targetCurrency.ToUpperInvariant() == "EUR" => 0.84m + (decimal)(random.NextDouble() * 0.12), // Different from API1 & API2
            "USD" when targetCurrency.ToUpperInvariant() == "GBP" => 0.74m + (decimal)(random.NextDouble() * 0.12),
            "USD" when targetCurrency.ToUpperInvariant() == "DOP" => 54.8m + (decimal)(random.NextDouble() * 6.0),
            "EUR" when targetCurrency.ToUpperInvariant() == "USD" => 1.16m + (decimal)(random.NextDouble() * 0.12),
            "EUR" when targetCurrency.ToUpperInvariant() == "GBP" => 0.87m + (decimal)(random.NextDouble() * 0.12),
            "GBP" when targetCurrency.ToUpperInvariant() == "USD" => 1.31m + (decimal)(random.NextDouble() * 0.12),
            "GBP" when targetCurrency.ToUpperInvariant() == "EUR" => 1.13m + (decimal)(random.NextDouble() * 0.12),
            "DOP" when targetCurrency.ToUpperInvariant() == "USD" => 0.0182m + (decimal)(random.NextDouble() * 0.0018),
            _ => 1.0m + (decimal)(random.NextDouble() * 0.25) // Default rate with variation
        };
    }
}