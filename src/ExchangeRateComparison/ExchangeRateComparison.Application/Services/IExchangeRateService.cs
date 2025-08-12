using ExchangeRateComparison.Domain.Entities;

namespace ExchangeRateComparison.Application.Services;

/// <summary>
/// Application service for exchange rate operations
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Compares exchange rates from multiple providers and returns the best offer
    /// </summary>
    /// <param name="request">The exchange request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comparison result with the best offer and all provider responses</returns>
    Task<ExchangeComparisonResult> CompareExchangeRatesAsync(
        ExchangeRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health status of all registered exchange rate providers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary containing provider name and health status</returns>
    Task<Dictionary<string, bool>> GetProvidersHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all registered providers
    /// </summary>
    /// <returns>List of provider information</returns>
    Task<IEnumerable<ProviderInfo>> GetAvailableProvidersAsync();
}

/// <summary>
/// Information about an exchange rate provider
/// </summary>
public record ProviderInfo(
    string Name,
    bool IsAvailable,
    DateTime? LastHealthCheck = null,
    string? Description = null);