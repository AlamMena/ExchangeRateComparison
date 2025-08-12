
namespace ExchangeRateComparison.WebApi.DTOs;

/// <summary>
/// Response model for currency exchange rate comparison
/// </summary>
public class ExchangeRateResponse
{
    /// <summary>
    /// Status of the comparison process
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Original request information
    /// </summary>
    public ExchangeRequestInfo Input { get; set; } = new();

    /// <summary>
    /// Best exchange rate offer found
    /// </summary>
    public ExchangeOfferInfo? BestOffer { get; set; }

    /// <summary>
    /// All exchange rate offers from providers
    /// </summary>
    public List<ExchangeOfferInfo> AllOffers { get; set; } = new();

    /// <summary>
    /// When the comparison was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// How long the comparison took
    /// </summary>
    public string ProcessingDuration { get; set; } = string.Empty;

    /// <summary>
    /// Number of successful offers
    /// </summary>
    public int SuccessfulOffersCount { get; set; }

    /// <summary>
    /// Number of failed offers
    /// </summary>
    public int FailedOffersCount { get; set; }

    /// <summary>
    /// Total number of providers queried
    /// </summary>
    public int TotalProvidersQueried { get; set; }

    /// <summary>
    /// Savings information (if applicable)
    /// </summary>
    public SavingsInfo? Savings { get; set; }

    /// <summary>
    /// Performance metrics (optional)
    /// </summary>
    public PerformanceMetrics? Performance { get; set; }
}

/// <summary>
/// Information about the original exchange request
/// </summary>
public class ExchangeRequestInfo
{
    /// <summary>
    /// Source currency code
    /// </summary>
    public string SourceCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Target currency code
    /// </summary>
    public string TargetCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Amount to convert
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Information about an exchange rate offer from a provider
/// </summary>
public class ExchangeOfferInfo
{
    /// <summary>
    /// Name of the provider
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Converted amount in target currency
    /// </summary>
    public decimal ConvertedAmount { get; set; }

    /// <summary>
    /// Exchange rate used for conversion
    /// </summary>
    public decimal ExchangeRate { get; set; }

    /// <summary>
    /// Whether the offer was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Error message (if not successful)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the response was received
    /// </summary>
    public DateTime ResponseTime { get; set; }

    /// <summary>
    /// How long the provider took to respond
    /// </summary>
    public string ResponseDuration { get; set; } = string.Empty;

    /// <summary>
    /// Additional provider details (optional)
    /// </summary>
    public ProviderDetails? Details { get; set; }
}

/// <summary>
/// Additional details about a provider
/// </summary>
public class ProviderDetails
{
    /// <summary>
    /// Provider endpoint URL
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Response format used (JSON, XML, etc.)
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Whether the provider was available
    /// </summary>
    public bool IsAvailable { get; set; }
}

/// <summary>
/// Information about savings achieved by selecting the best offer
/// </summary>
public class SavingsInfo
{
    /// <summary>
    /// Absolute savings amount (best - worst)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Percentage savings
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Currency of the savings
    /// </summary>
    public string Currency { get; set; } = string.Empty;
}

/// <summary>
/// Performance metrics for the comparison process
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Total processing time in milliseconds
    /// </summary>
    public double TotalProcessingTimeMs { get; set; }

    /// <summary>
    /// Average provider response time in milliseconds
    /// </summary>
    public double AverageProviderResponseTimeMs { get; set; }

    /// <summary>
    /// Fastest provider response time in milliseconds
    /// </summary>
    public double FastestProviderResponseTimeMs { get; set; }

    /// <summary>
    /// Slowest provider response time in milliseconds
    /// </summary>
    public double SlowestProviderResponseTimeMs { get; set; }

    /// <summary>
    /// Number of providers that responded successfully
    /// </summary>
    public int SuccessfulProvidersCount { get; set; }

    /// <summary>
    /// Number of providers that failed or timed out
    /// </summary>
    public int FailedProvidersCount { get; set; }
}