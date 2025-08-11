using ExchangeRateComparison.Domain.Interfaces;
using ExchangeRateComparison.Infrastructure.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace ExchangeRateComparison.Infrastructure.Extensions;


/// <summary>
/// Extension methods for provider factory registration
/// </summary>
public static class ProviderFactoryExtensions
{
    /// <summary>
    /// Registers the exchange rate provider factory
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddExchangeRateProviderFactory(this IServiceCollection services)
    {
        services.AddScoped<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();
        return services;
    }

    /// <summary>
    /// Registers providers using the factory
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="useHttpProviders">Whether to include HTTP providers</param>
    /// <param name="useMockProviders">Whether to include mock providers</param>
    /// <param name="mockConfig">Mock provider configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddProvidersFromFactory(
        this IServiceCollection services,
        bool useHttpProviders = true,
        bool useMockProviders = false,
        MockProvidersConfiguration? mockConfig = null)
    {
        services.AddScoped(serviceProvider =>
        {
            var factory = serviceProvider.GetRequiredService<IExchangeRateProviderFactory>();
            var providers = new List<IExchangeRateProvider>();

            if (useHttpProviders)
            {
                providers.AddRange(factory.CreateHttpProviders());
            }

            if (useMockProviders)
            {
                providers.AddRange(factory.CreateMockProviders(mockConfig));
            }

            return providers.AsEnumerable();
        });

        return services;
    }
}