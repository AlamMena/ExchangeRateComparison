using ExchangeRateComparison.Domain.Entities;
using ExchangeRateComparison.WebApi.DTOs;

namespace ExchangeRateComparison.WebApi.Extensions;

/// <summary>
/// Extension methods for mapping domain entities to API response models
/// </summary>
public static class DomainToApiExtensions
{
    /// <summary>
    /// Converts an ExchangeComparisonResult to an ExchangeRateResponse
    /// </summary>
    public static ExchangeRateResponse ToApiResponse(
        this ExchangeComparisonResult result, 
        bool includeProviderDetails = true,
        bool includePerformanceMetrics = false)
    {
        var response = new ExchangeRateResponse
        {
            Status = result.Status.ToString(),
            Input = new ExchangeRequestInfo
            {
                SourceCurrency = result.Input.SourceCurrency,
                TargetCurrency = result.Input.TargetCurrency,
                Amount = result.Input.Amount
            },
            BestOffer = result.BestOffer?.ToApiOfferInfo(includeProviderDetails),
            AllOffers = result.AllOffers.Select(offer => offer.ToApiOfferInfo(includeProviderDetails)).ToList(),
            ProcessedAt = result.ProcessedAt,
            ProcessingDuration = FormatDuration(result.ProcessingDuration),
            SuccessfulOffersCount = result.SuccessfulOffersCount,
            FailedOffersCount = result.FailedOffersCount,
            TotalProvidersQueried = result.AllOffers.Count
        };

        // Add savings information if there are multiple successful offers
        if (result.SuccessfulOffersCount > 1)
        {
            response.Savings = new SavingsInfo
            {
                Amount = result.CalculateSavings(),
                Percentage = result.CalculateSavingsPercentage(),
                Currency = result.Input.TargetCurrency
            };
        }

        // Add performance metrics if requested
        if (includePerformanceMetrics)
        {
            response.Performance = CreatePerformanceMetrics(result);
        }

        return response;
    }

    /// <summary>
    /// Converts an ExchangeRateOffer to an ExchangeOfferInfo
    /// </summary>
    public static ExchangeOfferInfo ToApiOfferInfo(this ExchangeRateOffer offer, bool includeDetails = true)
    {
        var offerInfo = new ExchangeOfferInfo
        {
            ProviderName = offer.ProviderName,
            ConvertedAmount = offer.ConvertedAmount,
            ExchangeRate = offer.ExchangeRate,
            IsSuccessful = offer.IsSuccessful,
            ErrorMessage = offer.ErrorMessage,
            ResponseTime = offer.ResponseTime,
            ResponseDuration = FormatDuration(offer.ResponseDuration)
        };

        if (includeDetails)
        {
            offerInfo.Details = new ProviderDetails
            {
                IsAvailable = offer.IsSuccessful,
                Format = DetermineProviderFormat(offer.ProviderName)
            };
        }

        return offerInfo;
    }

    /// <summary>
    /// Creates performance metrics from the comparison result
    /// </summary>
    private static PerformanceMetrics CreatePerformanceMetrics(ExchangeComparisonResult result)
    {
        var successfulOffers = result.AllOffers.Where(o => o.IsSuccessful).ToList();
        var responseTimes = result.AllOffers.Select(o => o.ResponseDuration.TotalMilliseconds).ToList();

        return new PerformanceMetrics
        {
            TotalProcessingTimeMs = result.ProcessingDuration.TotalMilliseconds,
            AverageProviderResponseTimeMs = responseTimes.Any() ? responseTimes.Average() : 0,
            FastestProviderResponseTimeMs = responseTimes.Any() ? responseTimes.Min() : 0,
            SlowestProviderResponseTimeMs = responseTimes.Any() ? responseTimes.Max() : 0,
            SuccessfulProvidersCount = successfulOffers.Count,
            FailedProvidersCount = result.AllOffers.Count - successfulOffers.Count
        };
    }

    /// <summary>
    /// Determines the data format used by a provider based on its name
    /// </summary>
    private static string DetermineProviderFormat(string providerName)
    {
        return providerName.ToUpperInvariant() switch
        {
            "API1" => "JSON",
            "API2" => "XML",
            "API3" => "JSON (Nested)",
            var name when name.Contains("JSON") => "JSON",
            var name when name.Contains("XML") => "XML",
            var name when name.Contains("MOCK") => "Mock",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Formats a TimeSpan as a human-readable duration string
    /// </summary>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMilliseconds < 1000)
        {
            return $"{duration.TotalMilliseconds:F0}ms";
        }
        
        if (duration.TotalSeconds < 60)
        {
            return $"{duration.TotalSeconds:F2}s";
        }
        
        return duration.ToString(@"mm\:ss\.fff");
    }
}

/// <summary>
/// Extension methods for HttpContext
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the correlation ID from the HttpContext
    /// </summary>
    public static string GetCorrelationId(this HttpContext context)
    {
        return context.Items["CorrelationId"]?.ToString() 
               ?? context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
               ?? context.Request.Headers["X-Request-ID"].FirstOrDefault()
               ?? Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>
    /// Sets the correlation ID in the HttpContext
    /// </summary>
    public static void SetCorrelationId(this HttpContext context, string correlationId)
    {
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
    }

    /// <summary>
    /// Gets the client IP address from the HttpContext
    /// </summary>
    public static string GetClientIpAddress(this HttpContext context)
    {
        return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
               ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
               ?? context.Connection.RemoteIpAddress?.ToString()
               ?? "Unknown";
    }

    /// <summary>
    /// Checks if the request is from a health check endpoint
    /// </summary>
    public static bool IsHealthCheckRequest(this HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        return path != null && (
            path.Contains("/health") || 
            path.Contains("/status") || 
            path.Contains("/ping"));
    }

    /// <summary>
    /// Gets the user agent from the request headers
    /// </summary>
    public static string GetUserAgent(this HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    }
}

/// <summary>
/// Extension methods for API validation
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates currency code format
    /// </summary>
    public static bool IsValidCurrencyCode(this string currencyCode)
    {
        return !string.IsNullOrWhiteSpace(currencyCode) 
               && currencyCode.Length == 3 
               && currencyCode.All(char.IsLetter);
    }

    /// <summary>
    /// Normalizes currency code to uppercase
    /// </summary>
    public static string NormalizeCurrencyCode(this string currencyCode)
    {
        return currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Validates amount range
    /// </summary>
    public static bool IsValidAmount(this decimal amount)
    {
        return amount > 0 && amount <= 1_000_000_000m;
    }
}

/// <summary>
/// Extension methods for response formatting
/// </summary>
public static class ResponseExtensions
{
    /// <summary>
    /// Adds standard API headers to the response
    /// </summary>
    public static void AddStandardApiHeaders(this HttpResponse response, string? correlationId = null)
    {
        response.Headers["X-API-Version"] = "1.0";
        response.Headers["X-Service"] = "ExchangeRateComparison";
        
        if (!string.IsNullOrEmpty(correlationId))
        {
            response.Headers["X-Correlation-ID"] = correlationId;
        }
        
        response.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("O");
    }

    /// <summary>
    /// Sets cache control headers for API responses
    /// </summary>
    public static void SetCacheHeaders(this HttpResponse response, TimeSpan maxAge)
    {
        response.Headers["Cache-Control"] = $"public, max-age={maxAge.TotalSeconds:F0}";
        response.Headers["Expires"] = DateTime.UtcNow.Add(maxAge).ToString("R");
    }

    /// <summary>
    /// Sets no-cache headers for sensitive responses
    /// </summary>
    public static void SetNoCacheHeaders(this HttpResponse response)
    {
        response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        response.Headers["Pragma"] = "no-cache";
        response.Headers["Expires"] = "0";
    }
}