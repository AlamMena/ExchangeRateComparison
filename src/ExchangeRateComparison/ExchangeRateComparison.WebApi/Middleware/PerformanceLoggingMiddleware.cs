using System.Diagnostics;

namespace ExchangeRateComparison.WebApi.Middleware;

/// <summary>
/// Middleware for logging performance metrics of HTTP requests
/// </summary>
public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;
    private readonly PerformanceOptions _options;

    public PerformanceLoggingMiddleware(
        RequestDelegate next, 
        ILogger<PerformanceLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _options = configuration.GetSection("Performance").Get<PerformanceOptions>() ?? new PerformanceOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            await LogPerformanceAsync(context, stopwatch.Elapsed, startTime);
        }
    }

    private async Task LogPerformanceAsync(HttpContext context, TimeSpan elapsed, DateTime startTime)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString();
        var elapsedMs = elapsed.TotalMilliseconds;

        // Determine if this is a slow request
        var isSlowRequest = elapsedMs > _options.SlowRequestThresholdMs;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId ?? "Unknown",
            ["RequestPath"] = context.Request.Path,
            ["RequestMethod"] = context.Request.Method
        });

        var performanceData = new
        {
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            QueryString = context.Request.QueryString.Value,
            StatusCode = context.Response.StatusCode,
            ElapsedMs = Math.Round(elapsedMs, 2),
            StartTime = startTime,
            EndTime = startTime.Add(elapsed),
            UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
            RemoteIP = GetClientIpAddress(context),
            ResponseSize = GetResponseSize(context),
            IsSlowRequest = isSlowRequest,
            CorrelationId = correlationId
        };

        // Log based on performance and status
        if (isSlowRequest)
        {
            _logger.LogWarning("Slow request detected: {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode})",
                performanceData.Method,
                performanceData.Path,
                performanceData.ElapsedMs,
                performanceData.StatusCode);
        }
        else if (_options.LogAllRequests)
        {
            _logger.LogInformation("Request performance: {Method} {Path} completed in {ElapsedMs}ms (Status: {StatusCode})",
                performanceData.Method,
                performanceData.Path,
                performanceData.ElapsedMs,
                performanceData.StatusCode);
        }

        // Log detailed performance data for debug level
        _logger.LogDebug("Detailed performance data: {@PerformanceData}", performanceData);

        // Add performance headers to response
        if (_options.AddPerformanceHeaders)
        {
            context.Response.Headers["X-Response-Time-Ms"] = elapsedMs.ToString("F2");
            context.Response.Headers["X-Request-Start-Time"] = startTime.ToString("O");
            
            if (isSlowRequest)
            {
                context.Response.Headers["X-Slow-Request"] = "true";
            }
        }

        // Store metrics for potential aggregation
        if (_options.EnableMetricsCollection)
        {
            await RecordMetricsAsync(performanceData);
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
               ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
               ?? context.Connection.RemoteIpAddress?.ToString()
               ?? "Unknown";
    }

    private static long? GetResponseSize(HttpContext context)
    {
        return context.Response.Headers.ContentLength
               ?? (context.Response.Body.CanSeek ? context.Response.Body.Length : null);
    }

    private async Task RecordMetricsAsync(object performanceData)
    {
        try
        {
            // Here you could send metrics to a monitoring system like:
            // - Application Insights
            // - Prometheus
            // - Custom metrics endpoint
            // - Database for analysis
            
            // For now, just log to debug
            _logger.LogDebug("Recording metrics: {@Metrics}", performanceData);
            
            // Simulate async operation
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Don't let metrics recording failures affect the request
            _logger.LogWarning(ex, "Failed to record performance metrics");
        }
    }
}

/// <summary>
/// Configuration options for performance logging
/// </summary>
public class PerformanceOptions
{
    /// <summary>
    /// Threshold in milliseconds to consider a request as slow (default: 1000ms)
    /// </summary>
    public double SlowRequestThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Whether to log all requests or only slow ones (default: false)
    /// </summary>
    public bool LogAllRequests { get; set; } = false;

    /// <summary>
    /// Whether to add performance headers to responses (default: true)
    /// </summary>
    public bool AddPerformanceHeaders { get; set; } = true;

    /// <summary>
    /// Whether to enable metrics collection for external systems (default: false)
    /// </summary>
    public bool EnableMetricsCollection { get; set; } = false;

    /// <summary>
    /// List of paths to exclude from performance logging
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/metrics",
        "/favicon.ico"
    };
}