using ExchangeRateComparison.Domain.Entities;

namespace ExchangeRateComparison.Domain.Interfaces;

/// <summary>
/// Interface for exchange rate providers that can fetch currency conversion offers
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Gets the unique name of the provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Indicates whether this provider is currently available for requests
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets an exchange rate offer for the given request
    /// </summary>
    /// <param name="request">The exchange request containing source currency, target currency, and amount</param>
    /// <param name="cancellationToken">Cancellation token for timeout and cancellation support</param>
    /// <returns>
    /// Exchange rate offer containing the conversion result, or null if the provider is unavailable.
    /// If an error occurs, returns a failed offer with error details.
    /// </returns>
    Task<ExchangeRateOffer?> GetOfferAsync(ExchangeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check to verify the provider is working correctly
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for timeout support</param>
    /// <returns>True if the provider is healthy and can process requests</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}