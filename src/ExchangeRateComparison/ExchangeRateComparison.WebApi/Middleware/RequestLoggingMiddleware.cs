using System.Diagnostics;

namespace ExchangeRateComparison.WebApi.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate correlation ID if not present
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                          ?? context.Request.Headers["X-Request-ID"].FirstOrDefault()
                          ?? Guid.NewGuid().ToString("N")[..8];

        // Add correlation ID to response headers
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        
        // Add correlation ID to HttpContext for use in controllers
        context.Items["CorrelationId"] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestId"] = context.TraceIdentifier
        });

        try
        {
            // Log request start
            _logger.LogInformation("HTTP {Method} {Path} started from {RemoteIP} with correlation ID {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                GetClientIpAddress(context),
                correlationId);

            // Log request headers (selective)
            LogRequestHeaders(context);

            // Continue pipeline
            await _next(context);

            stopwatch.Stop();

            // Log successful completion
            _logger.LogInformation("HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log error
            _logger.LogError(ex, "HTTP {Method} {Path} failed with exception after {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);

            throw; // Re-throw to let error handling middleware handle it
        }
    }

    private void LogRequestHeaders(HttpContext context)
    {
        var relevantHeaders = new[]
        {
            "User-Agent", "Accept", "Accept-Language", "Content-Type", 
            "Content-Length", "Authorization", "X-API-Key"
        };

        var headers = new Dictionary<string, string>();
        
        foreach (var headerName in relevantHeaders)
        {
            if (context.Request.Headers.TryGetValue(headerName, out var headerValue))
            {
                // Mask sensitive headers
                var maskedValue = headerName switch
                {
                    "Authorization" => MaskValue(headerValue.ToString()),
                    "X-API-Key" => MaskValue(headerValue.ToString()),
                    _ => headerValue.ToString()
                };
                
                headers[headerName] = maskedValue;
            }
        }

        if (headers.Any())
        {
            _logger.LogDebug("Request headers: {@Headers}", headers);
        }
    }

    private static string MaskValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= 8)
        {
            return "***";
        }

        var start = value[..4];
        var end = value[^4..];
        var middle = new string('*', Math.Min(8, value.Length - 8));
        
        return $"{start}{middle}{end}";
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (load balancer/proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}