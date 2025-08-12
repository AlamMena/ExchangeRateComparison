using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace MockApi2.XmlProvider.Controllers;

[ApiController]
[Route("[controller]")]
[Tags("Exchange")]
public class ConvertController : ControllerBase
{
    private readonly ILogger<ConvertController> _logger;

    public ConvertController(ILogger<ConvertController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Convert currency using XML format
    /// </summary>
    /// <param name="delay">Optional delay in milliseconds for testing</param>
    /// <param name="fail">Set to true to simulate failure</param>
    /// <returns>XML response with conversion result</returns>
    [HttpPost]
    [Consumes("application/xml", "text/xml")]
    [Produces("application/xml")]
    public async Task<IActionResult> Post(
        [FromQuery] int? delay,
        [FromQuery] bool fail = false)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Endpoint"] = "/convert"
        });

        try
        {
            // Read XML request body
            string requestXml;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                requestXml = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(requestXml))
            {
                _logger.LogWarning("API2: Empty request body");
                
                var errorXml = CreateErrorXml("Bad Request", "Request body cannot be empty");
                return BadRequest(errorXml);
            }

            _logger.LogDebug("API2: Received XML request: {RequestXml}", requestXml);

            // Parse XML request
            var xmlDoc = XDocument.Parse(requestXml);
            var xmlRoot = xmlDoc.Root;

            if (xmlRoot?.Name != "XML")
            {
                _logger.LogWarning("API2: Invalid XML root element: {RootName}", xmlRoot?.Name);
                
                var errorXml = CreateErrorXml("Invalid XML format", "Root element must be 'XML'");
                return BadRequest(errorXml);
            }

            var fromElement = xmlRoot.Element("From");
            var toElement = xmlRoot.Element("To");
            var amountElement = xmlRoot.Element("Amount");

            if (fromElement == null || toElement == null || amountElement == null)
            {
                _logger.LogWarning("API2: Missing required XML elements");
                
                var errorXml = CreateErrorXml("Missing required elements", "XML must contain From, To, and Amount elements");
                return BadRequest(errorXml);
            }

            var from = fromElement.Value;
            var to = toElement.Value;
            var amountText = amountElement.Value;

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || 
                !decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ||
                amount <= 0)
            {
                _logger.LogWarning("API2: Invalid values - From: {From}, To: {To}, Amount: {Amount}", 
                    from, to, amountText);
                
                var errorXml = CreateErrorXml("Invalid values", "From and To must be valid currencies and Amount must be a positive number");
                return BadRequest(errorXml);
            }

            _logger.LogInformation("API2: Processing conversion request - {From} to {To}, Amount: {Amount}", 
                from, to, amount);

            // Check for unsupported currencies (simulate some API limitations)
            var unsupportedCurrencies = new[] { "XXX", "YYY", "ZZZ" };
            if (unsupportedCurrencies.Contains(from.ToUpperInvariant()) || 
                unsupportedCurrencies.Contains(to.ToUpperInvariant()))
            {
                _logger.LogWarning("API2: Unsupported currency - From: {From}, To: {To}", from, to);
                
                var errorXml = CreateErrorXml("Unsupported currency", $"Currency pair {from}/{to} is not supported");
                return BadRequest(errorXml);
            }

            // Simulate processing delay (configurable)
            var delayMs = delay ?? Random.Shared.Next(75, 300);
            
            if (delayMs > 0)
            {
                _logger.LogDebug("API2: Simulating {Delay}ms processing delay", delayMs);
                await Task.Delay(delayMs);
            }

            // Simulate random failures (if requested)
            if (fail)
            {
                _logger.LogWarning("API2: Simulating random failure");
                
                var errorXml = CreateErrorXml("Service temporarily unavailable", "Simulated random failure for testing");
                return StatusCode(503, errorXml);
            }

            // Calculate mock exchange rate and converted amount
            var rate = CalculateExchangeRate(from, to);
            var result = amount * rate;
            
            // Create XML response
            var responseXml = CreateSuccessXml(result);
            
            _logger.LogInformation("API2: Returning result {Result:F2} (rate {Rate:F4}) for {From}/{To} in {Delay}ms", 
                result, rate, from, to, delayMs);

            return Content(responseXml, "application/xml");
        }
        catch (System.Xml.XmlException ex)
        {
            _logger.LogError(ex, "API2: XML parsing error");
            
            var errorXml = CreateErrorXml("XML parsing error", ex.Message);
            return BadRequest(errorXml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API2: Unexpected error");
            
            var errorXml = CreateErrorXml("Internal server error", ex.Message);
            return StatusCode(500, errorXml);
        }
    }

    /// <summary>
    /// Get service information
    /// </summary>
    /// <returns>Service details and usage information</returns>
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        _logger.LogDebug("API2: Info endpoint requested");
        
        return Ok(new
        {
            service = "Mock API2 - XML Exchange Rate Provider",
            version = "1.0.0",
            format = "XML",
            port = 5002,
            endpoints = new
            {
                convert = "POST /convert - <XML><From/><To/><Amount/></XML> → <XML><Result/></XML>",
                health = "GET /health - Health check",
                info = "GET /convert/info - Service information"
            },
            sampleRequest = new
            {
                method = "POST",
                url = "/convert",
                contentType = "application/xml",
                body = "<XML><From>USD</From><To>EUR</To><Amount>100</Amount></XML>"
            },
            sampleResponse = new
            {
                contentType = "application/xml",
                body = "<XML><Result>92.50</Result></XML>"
            },
            queryParameters = new
            {
                delay = "Optional delay in milliseconds (e.g., ?delay=1000)",
                fail = "Set to 'true' to simulate failure (e.g., ?fail=true)"
            }
        });
    }

    /// <summary>
    /// Create error XML response
    /// </summary>
    private static string CreateErrorXml(string error, string message)
    {
        var errorXml = new XElement("XML",
            new XElement("Error", error),
            new XElement("Message", message)
        );
        
        return errorXml.ToString();
    }

    /// <summary>
    /// Create success XML response
    /// </summary>
    private static string CreateSuccessXml(decimal result)
    {
        var responseXml = new XElement("XML",
            new XElement("Result", result.ToString("F2", CultureInfo.InvariantCulture))
        );
        
        return responseXml.ToString();
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
            "USD" when to.ToUpperInvariant() == "EUR" => 0.87m + (decimal)(random.NextDouble() * 0.08), // Slightly different from API1
            "USD" when to.ToUpperInvariant() == "GBP" => 0.77m + (decimal)(random.NextDouble() * 0.08),
            "USD" when to.ToUpperInvariant() == "DOP" => 56.0m + (decimal)(random.NextDouble() * 4.0),
            "EUR" when to.ToUpperInvariant() == "USD" => 1.17m + (decimal)(random.NextDouble() * 0.08),
            "EUR" when to.ToUpperInvariant() == "GBP" => 0.90m + (decimal)(random.NextDouble() * 0.08),
            "GBP" when to.ToUpperInvariant() == "USD" => 1.32m + (decimal)(random.NextDouble() * 0.08),
            "GBP" when to.ToUpperInvariant() == "EUR" => 1.14m + (decimal)(random.NextDouble() * 0.08),
            "DOP" when to.ToUpperInvariant() == "USD" => 0.0175m + (decimal)(random.NextDouble() * 0.0015),
            _ => 1.0m + (decimal)(random.NextDouble() * 0.15) // Default rate with variation
        };
    }
}