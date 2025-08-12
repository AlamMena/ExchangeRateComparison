using ExchangeRateComparison.Domain.Interfaces;
using ExchangeRateComparison.Infrastructure.Extensions;

namespace ExchangeRateComparison.Infrastructure.Factories;

/// <summary>
/// Factory for creating exchange rate provider instances
/// </summary>
public interface IExchangeRateProviderFactory
{
    /// <summary>
    /// Creates all configured HTTP providers
    /// </summary>
    /// <returns>Collection of HTTP providers</returns>
    IEnumerable<IExchangeRateProvider> CreateHttpProviders();

    /// <summary>
    /// Creates all configured mock providers
    /// </summary>
    /// <param name="configuration">Mock provider configuration</param>
    /// <returns>Collection of mock providers</returns>
    IEnumerable<IExchangeRateProvider> CreateMockProviders(MockProvidersConfiguration? configuration = null);

    /// <summary>
    /// Creates a specific provider by name
    /// </summary>
    /// <param name="providerName">Name of the provider to create</param>
    /// <returns>Provider instance or null if not found</returns>
    IExchangeRateProvider? CreateProvider(string providerName);

    /// <summary>
    /// Gets all available provider names
    /// </summary>
    /// <returns>List of provider names</returns>
    IEnumerable<string> GetAvailableProviderNames();
}


