using ExchangeRateComparison.Domain.Interfaces;
using ExchangeRateComparison.Infrastructure.Configuration;
using ExchangeRateComparison.Infrastructure.Extensions;
using ExchangeRateComparison.Infrastructure.Providers;
using ExchangeRateComparison.Infrastructure.Providers.MockProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExchangeRateComparison.Infrastructure.Factories;

/// <summary>
/// Default implementation of exchange rate provider factory
/// </summary>
public class ExchangeRateProviderFactory : IExchangeRateProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExchangeRateProviderFactory> _logger;
    private readonly ApiProviderSettings _settings;

    public ExchangeRateProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<ExchangeRateProviderFactory> logger,
        IOptions<ApiProviderSettings> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Creates all configured HTTP providers
    /// </summary>
    public IEnumerable<IExchangeRateProvider> CreateHttpProviders()
    {
        var providers = new List<IExchangeRateProvider>();

        try
        {
            // Create API1 provider if enabled
            if (_settings.Api1.IsEnabled)
            {
                var api1Provider = CreateApi1Provider();
                if (api1Provider != null)
                {
                    providers.Add(api1Provider);
                    _logger.LogDebug("Created API1 provider: {ProviderName}", api1Provider.ProviderName);
                }
            }

            // Create API2 provider if enabled
            if (_settings.Api2.IsEnabled)
            {
                var api2Provider = CreateApi2Provider();
                if (api2Provider != null)
                {
                    providers.Add(api2Provider);
                    _logger.LogDebug("Created API2 provider: {ProviderName}", api2Provider.ProviderName);
                }
            }

            // Create API3 provider if enabled
            if (_settings.Api3.IsEnabled)
            {
                var api3Provider = CreateApi3Provider();
                if (api3Provider != null)
                {
                    providers.Add(api3Provider);
                    _logger.LogDebug("Created API3 provider: {ProviderName}", api3Provider.ProviderName);
                }
            }

            _logger.LogInformation("Created {ProviderCount} HTTP providers", providers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating HTTP providers");
            throw;
        }

        return providers;
    }

    /// <summary>
    /// Creates all configured mock providers
    /// </summary>
    public IEnumerable<IExchangeRateProvider> CreateMockProviders(MockProvidersConfiguration? configuration = null)
    {
        var config = configuration ?? new MockProvidersConfiguration();
        var providers = new List<IExchangeRateProvider>();

        try
        {
            if (config.IncludeSuccessProvider)
            {
                var successProvider = CreateMockSuccessProvider(config.SuccessSettings);
                providers.Add(successProvider);
                _logger.LogDebug("Created mock success provider: {ProviderName}", successProvider.ProviderName);
            }

            if (config.IncludeTimeoutProvider)
            {
                var timeoutProvider = CreateMockTimeoutProvider(config.TimeoutSettings);
                providers.Add(timeoutProvider);
                _logger.LogDebug("Created mock timeout provider: {ProviderName}", timeoutProvider.ProviderName);
            }

            if (config.IncludeFailureProvider)
            {
                var failureProvider = CreateMockFailureProvider(config.FailureSettings);
                providers.Add(failureProvider);
                _logger.LogDebug("Created mock failure provider: {ProviderName}", failureProvider.ProviderName);
            }

            _logger.LogInformation("Created {ProviderCount} mock providers", providers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mock providers");
            throw;
        }

        return providers;
    }

    /// <summary>
    /// Creates a specific provider by name
    /// </summary>
    public IExchangeRateProvider? CreateProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));

        try
        {
            return providerName.ToUpperInvariant() switch
            {
                "API1" => CreateApi1Provider(),
                "API2" => CreateApi2Provider(),
                "API3" => CreateApi3Provider(),
                "MOCKSUCCESS" => CreateMockSuccessProvider(),
                "MOCKTIMEOUT" => CreateMockTimeoutProvider(),
                "MOCKFAILURE" => CreateMockFailureProvider(),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provider: {ProviderName}", providerName);
            return null;
        }
    }

    /// <summary>
    /// Gets all available provider names
    /// </summary>
    public IEnumerable<string> GetAvailableProviderNames()
    {
        var names = new List<string>();

        if (_settings.Api1.IsEnabled) names.Add("API1");
        if (_settings.Api2.IsEnabled) names.Add("API2");
        if (_settings.Api3.IsEnabled) names.Add("API3");

        // Add mock provider names
        names.AddRange(new[] { "MockSuccess", "MockTimeout", "MockFailure" });

        return names;
    }

    private Api1JsonHttpProvider? CreateApi1Provider()
    {
        try
        {
            var httpClient = _serviceProvider.GetRequiredService<IHttpClientFactory>()
                .CreateClient(nameof(Api1JsonHttpProvider));
            
            var logger = _serviceProvider.GetRequiredService<ILogger<Api1JsonHttpProvider>>();
            var options = _serviceProvider.GetRequiredService<IOptions<ApiProviderSettings>>();

            return new Api1JsonHttpProvider(httpClient, logger, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API1 provider");
            return null;
        }
    }

    private Api2XmlHttpProvider? CreateApi2Provider()
    {
        try
        {
            var httpClient = _serviceProvider.GetRequiredService<IHttpClientFactory>()
                .CreateClient(nameof(Api2XmlHttpProvider));
            
            var logger = _serviceProvider.GetRequiredService<ILogger<Api2XmlHttpProvider>>();
            var options = _serviceProvider.GetRequiredService<IOptions<ApiProviderSettings>>();

            return new Api2XmlHttpProvider(httpClient, logger, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API2 provider");
            return null;
        }
    }

    private Api3JsonHttpProvider? CreateApi3Provider()
    {
        try
        {
            var httpClient = _serviceProvider.GetRequiredService<IHttpClientFactory>()
                .CreateClient(nameof(Api3JsonHttpProvider));
            
            var logger = _serviceProvider.GetRequiredService<ILogger<Api3JsonHttpProvider>>();
            var options = _serviceProvider.GetRequiredService<IOptions<ApiProviderSettings>>();

            return new Api3JsonHttpProvider(httpClient, logger, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API3 provider");
            return null;
        }
    }

    private MockSuccessProvider CreateMockSuccessProvider(MockProviderSettings? settings = null)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<MockSuccessProvider>>();
        return new MockSuccessProvider(logger, settings);
    }

    private MockTimeoutProvider CreateMockTimeoutProvider(MockProviderSettings? settings = null)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<MockTimeoutProvider>>();
        return new MockTimeoutProvider(logger, settings);
    }

    private MockFailureProvider CreateMockFailureProvider(MockProviderSettings? settings = null)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<MockFailureProvider>>();
        return new MockFailureProvider(logger, settings);
    }
}
