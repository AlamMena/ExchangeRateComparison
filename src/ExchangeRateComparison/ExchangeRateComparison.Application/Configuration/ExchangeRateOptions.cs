namespace ExchangeRateComparison.Application.Configuration;

/// <summary>
/// Configuration options for exchange rate comparison
/// </summary>
public class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRate";

    /// <summary>
    /// Timeout for individual provider requests in seconds (default: 3 seconds)
    /// </summary>
    public int ProviderTimeoutSeconds { get; set; } = 3;

    /// <summary>
    /// Maximum number of concurrent provider requests (default: 10)
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Whether to fail fast if all providers fail (default: false - always return Completed)
    /// </summary>
    public bool FailFastOnAllProviderFailures { get; set; } = false;

    /// <summary>
    /// Enable detailed performance logging (default: true)
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;

    /// <summary>
    /// Minimum number of successful offers required to consider the process successful
    /// </summary>
    public int MinimumSuccessfulOffers { get; set; } = 1;

    /// <summary>
    /// Gets the provider timeout as TimeSpan
    /// </summary>
    public TimeSpan ProviderTimeout => TimeSpan.FromSeconds(ProviderTimeoutSeconds);

    /// <summary>
    /// Validates the configuration values
    /// </summary>
    public void Validate()
    {
        if (ProviderTimeoutSeconds <= 0)
            throw new InvalidOperationException("ProviderTimeoutSeconds must be greater than 0");

        if (MaxConcurrentRequests <= 0)
            throw new InvalidOperationException("MaxConcurrentRequests must be greater than 0");

        if (MinimumSuccessfulOffers < 0)
            throw new InvalidOperationException("MinimumSuccessfulOffers must be greater than or equal to 0");
    }
}