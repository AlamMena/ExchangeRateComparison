using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ExchangeRateComparison.Infrastructure.Common;

/// <summary>
/// Utility methods for HTTP operations and error handling
/// </summary>
public static class HttpUtilities
{
    /// <summary>
    /// Creates a JSON StringContent with UTF-8 encoding
    /// </summary>
    /// <param name="data">Object to serialize to JSON</param>
    /// <param name="options">JSON serializer options</param>
    /// <returns>StringContent ready for HTTP requests</returns>
    public static StringContent CreateJsonContent<T>(T data, JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize(data, options ?? JsonSerializerOptions.Default);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Creates an XML StringContent with UTF-8 encoding
    /// </summary>
    /// <param name="xmlContent">XML content as string</param>
    /// <returns>StringContent ready for HTTP requests</returns>
    public static StringContent CreateXmlContent(string xmlContent)
    {
        return new StringContent(xmlContent, Encoding.UTF8, "application/xml");
    }

    /// <summary>
    /// Safely reads response content as string with error handling
    /// </summary>
    /// <param name="response">HTTP response message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response content as string</returns>
    public static async Task<string> ReadResponseContentSafelyAsync(
        HttpResponseMessage response, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception)
        {
            // If we can't read the content, return a placeholder
            return $"[Unable to read response content - Status: {response.StatusCode}]";
        }
    }

    /// <summary>
    /// Determines if an HTTP status code indicates a temporary failure that could be retried
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <returns>True if the failure might be temporary</returns>
    public static bool IsTemporaryFailure(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a user-friendly error message for HTTP status codes
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="reasonPhrase">Optional reason phrase from response</param>
    /// <returns>User-friendly error message</returns>
    public static string GetFriendlyErrorMessage(HttpStatusCode statusCode, string? reasonPhrase = null)
    {
        var baseMessage = statusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request format",
            HttpStatusCode.Unauthorized => "Authentication required",
            HttpStatusCode.Forbidden => "Access denied",
            HttpStatusCode.NotFound => "Service endpoint not found",
            HttpStatusCode.RequestTimeout => "Request timed out",
            HttpStatusCode.TooManyRequests => "Rate limit exceeded",
            HttpStatusCode.InternalServerError => "Internal server error",
            HttpStatusCode.BadGateway => "Bad gateway",
            HttpStatusCode.ServiceUnavailable => "Service temporarily unavailable",
            HttpStatusCode.GatewayTimeout => "Gateway timeout",
            _ => $"HTTP {(int)statusCode} error"
        };

        return string.IsNullOrWhiteSpace(reasonPhrase) 
            ? baseMessage 
            : $"{baseMessage}: {reasonPhrase}";
    }

    /// <summary>
    /// Logs HTTP request details for debugging
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="method">HTTP method</param>
    /// <param name="url">Request URL</param>
    /// <param name="content">Request content (optional)</param>
    /// <param name="providerName">Provider name for context</param>
    public static void LogHttpRequest(
        ILogger logger, 
        HttpMethod method, 
        string url, 
        string? content = null,
        string? providerName = null)
    {
        var context = string.IsNullOrEmpty(providerName) ? "" : $"{providerName}: ";
        
        if (string.IsNullOrEmpty(content))
        {
            logger.LogDebug("{Context}Sending {Method} request to {Url}", context, method, url);
        }
        else
        {
            logger.LogDebug("{Context}Sending {Method} request to {Url} with content: {Content}", 
                context, method, url, content);
        }
    }

    /// <summary>
    /// Logs HTTP response details for debugging
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="response">HTTP response</param>
    /// <param name="content">Response content</param>
    /// <param name="duration">Request duration</param>
    /// <param name="providerName">Provider name for context</param>
    public static void LogHttpResponse(
        ILogger logger,
        HttpResponseMessage response,
        string content,
        TimeSpan duration,
        string? providerName = null)
    {
        var context = string.IsNullOrEmpty(providerName) ? "" : $"{providerName}: ";
        
        if (response.IsSuccessStatusCode)
        {
            logger.LogDebug(
                "{Context}Received {StatusCode} response in {Duration}ms: {Content}",
                context,
                (int)response.StatusCode,
                duration.TotalMilliseconds,
                content);
        }
        else
        {
            logger.LogWarning(
                "{Context}Received {StatusCode} error in {Duration}ms: {Content}",
                context,
                (int)response.StatusCode,
                duration.TotalMilliseconds,
                content);
        }
    }

    /// <summary>
    /// Executes an HTTP operation with timing and logging
    /// </summary>
    /// <param name="operation">HTTP operation to execute</param>
    /// <param name="logger">Logger for recording timing</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing result and duration</returns>
    public static async Task<(T Result, TimeSpan Duration)> ExecuteWithTimingAsync<T>(
        Func<Task<T>> operation,
        ILogger logger,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.LogDebug("Starting {OperationName}", operationName);
            
            var result = await operation();
            
            stopwatch.Stop();
            
            logger.LogDebug(
                "Completed {OperationName} in {Duration}ms", 
                operationName, 
                stopwatch.ElapsedMilliseconds);
            
            return (result, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            logger.LogError(ex, 
                "Failed {OperationName} after {Duration}ms", 
                operationName, 
                stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }

    /// <summary>
    /// Validates that a URL is properly formatted
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <param name="parameterName">Parameter name for exception</param>
    /// <returns>Validated URL</returns>
    /// <exception cref="ArgumentException">When URL is invalid</exception>
    public static string ValidateUrl(string url, string parameterName = "url")
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", parameterName);
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid URL format: {url}", parameterName);
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException($"URL must use HTTP or HTTPS protocol: {url}", parameterName);
        }

        return url;
    }

    /// <summary>
    /// Combines base URL and endpoint safely
    /// </summary>
    /// <param name="baseUrl">Base URL</param>
    /// <param name="endpoint">Endpoint path</param>
    /// <returns>Combined URL</returns>
    public static string CombineUrl(string baseUrl, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));
        
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));

        var trimmedBase = baseUrl.TrimEnd('/');
        var trimmedEndpoint = endpoint.TrimStart('/');
        
        return $"{trimmedBase}/{trimmedEndpoint}";
    }

    /// <summary>
    /// Creates a correlation ID for tracking requests
    /// </summary>
    /// <param name="prefix">Optional prefix for the correlation ID</param>
    /// <returns>Unique correlation ID</returns>
    public static string CreateCorrelationId(string? prefix = null)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        return string.IsNullOrEmpty(prefix) ? id : $"{prefix}-{id}";
    }

    /// <summary>
    /// Adds standard headers to HttpClient for API requests
    /// </summary>
    /// <param name="client">HttpClient to configure</param>
    /// <param name="apiKey">Optional API key</param>
    /// <param name="correlationId">Optional correlation ID</param>
    public static void AddStandardHeaders(HttpClient client, string? apiKey = null, string? correlationId = null)
    {
        // Remove existing headers to avoid duplicates
        client.DefaultRequestHeaders.Remove("X-API-Key");
        client.DefaultRequestHeaders.Remove("X-Correlation-ID");
        client.DefaultRequestHeaders.Remove("X-Request-ID");

        // Add API key if provided
        if (!string.IsNullOrEmpty(apiKey))
        {
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        // Add correlation ID if provided
        if (!string.IsNullOrEmpty(correlationId))
        {
            client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
            client.DefaultRequestHeaders.Add("X-Request-ID", correlationId);
        }
    }
}