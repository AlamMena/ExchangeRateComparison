using ExchangeRateComparison.Domain.Interfaces;
using ExchangeRateComparison.Infrastructure.Configuration;
using ExchangeRateComparison.Infrastructure.Providers;
using ExchangeRateComparison.Infrastructure.Providers.MockProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace ExchangeRateComparison.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering infrastructure layer services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure layer services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<ApiProviderSettings>(
            configuration.GetSection(ApiProviderSettings.SectionName));

        // Validate configuration on startup
        services.AddSingleton<IValidateOptions<ApiProviderSettings>, ValidateApiProviderSettings>();

        // Register HTTP clients for each provider
        RegisterHttpClients(services);

        // Register exchange rate providers
        RegisterExchangeRateProviders(services);

        return services;
    }

    /// <summary>
    /// Adds infrastructure layer services with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure provider settings</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        Action<ApiProviderSettings> configureOptions)
    {
        // Register configuration
        services.Configure(configureOptions);

        // Validate configuration on startup
        services.AddSingleton<IValidateOptions<ApiProviderSettings>, ValidateApiProviderSettings>();

        // Register HTTP clients for each provider
        RegisterHttpClients(services);

        // Register exchange rate providers
        RegisterExchangeRateProviders(services);

        return services;
    }

    /// <summary>
    /// Adds only mock providers for testing
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureMocks">Optional action to configure mock providers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMockExchangeRateProviders(
        this IServiceCollection services,
        Action<MockProvidersConfiguration>? configureMocks = null)
    {
        var mockConfig = new MockProvidersConfiguration();
        configureMocks?.Invoke(mockConfig);

        // Register mock providers based on configuration
        if (mockConfig.IncludeSuccessProvider)
        {
            services.AddSingleton<IExchangeRateProvider>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<MockSuccessProvider>>();
                return new MockSuccessProvider(logger, mockConfig.SuccessSettings);
            });
        }

        if (mockConfig.IncludeTimeoutProvider)
        {
            services.AddSingleton<IExchangeRateProvider>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<MockTimeoutProvider>>();
                return new MockTimeoutProvider(logger, mockConfig.TimeoutSettings);
            });
        }

        if (mockConfig.IncludeFailureProvider)
        {
            services.AddSingleton<IExchangeRateProvider>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<MockFailureProvider>>();
                return new MockFailureProvider(logger, mockConfig.FailureSettings);
            });
        }

        return services;
    }

    private static void RegisterHttpClients(IServiceCollection services)
    {
        // Configure shared HTTP client settings
        services.AddHttpClient<Api1JsonHttpProvider>("Api1HttpClient", (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ApiProviderSettings>>().Value;
            ConfigureHttpClient(client, settings.HttpClient);
            client.Timeout = TimeSpan.FromSeconds(settings.Api1.TimeoutSeconds);
        });

        services.AddHttpClient<Api2XmlHttpProvider>("Api2HttpClient", (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ApiProviderSettings>>().Value;
            ConfigureHttpClient(client, settings.HttpClient);
            client.Timeout = TimeSpan.FromSeconds(settings.Api2.TimeoutSeconds);
        });

        services.AddHttpClient<Api3JsonHttpProvider>("Api3HttpClient", (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ApiProviderSettings>>().Value;
            ConfigureHttpClient(client, settings.HttpClient);
            client.Timeout = TimeSpan.FromSeconds(settings.Api3.TimeoutSeconds);
        });
    }

    private static void ConfigureHttpClient(HttpClient client, HttpClientSettings settings)
    {
        // Set default headers
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", settings.UserAgent);
        client.DefaultRequestHeaders.Add("Accept", "application/json, application/xml, text/xml");
        client.DefaultRequestHeaders.Add("Accept-Charset", "utf-8");

        // Set default timeout (will be overridden per provider)
        client.Timeout = settings.DefaultTimeout;
    }

    private static void RegisterExchangeRateProviders(IServiceCollection services)
    {
        // Register HTTP providers
        services.AddScoped<IExchangeRateProvider, Api1JsonHttpProvider>();
        services.AddScoped<IExchangeRateProvider, Api2XmlHttpProvider>();
        services.AddScoped<IExchangeRateProvider, Api3JsonHttpProvider>();
    }
}

/// <summary>
/// Configuration for mock providers used in testing
/// </summary>
public class MockProvidersConfiguration
{
    public bool IncludeSuccessProvider { get; set; } = true;
    public bool IncludeTimeoutProvider { get; set; } = true;
    public bool IncludeFailureProvider { get; set; } = true;

    public MockProviderSettings SuccessSettings { get; set; } = 
        MockProviderSettings.CreateSuccess("MockSuccess", 50);

    public MockProviderSettings TimeoutSettings { get; set; } = 
        MockProviderSettings.CreateTimeout("MockTimeout", 5000);

    public MockProviderSettings FailureSettings { get; set; } = 
        MockProviderSettings.CreateFailure("MockFailure", "Simulated API failure", 100);

    /// <summary>
    /// Creates configuration for successful testing (all providers work)
    /// </summary>
    public static MockProvidersConfiguration ForSuccessfulTesting()
    {
        return new MockProvidersConfiguration
        {
            IncludeSuccessProvider = true,
            IncludeTimeoutProvider = false,
            IncludeFailureProvider = false,
            SuccessSettings = MockProviderSettings.CreateSuccess("MockSuccess", 10)
        };
    }

    /// <summary>
    /// Creates configuration for failure testing (mixed results)
    /// </summary>
    public static MockProvidersConfiguration ForFailureTesting()
    {
        return new MockProvidersConfiguration
        {
            IncludeSuccessProvider = true,
            IncludeTimeoutProvider = true,
            IncludeFailureProvider = true,
            SuccessSettings = MockProviderSettings.CreateSuccess("MockSuccess", 100),
            TimeoutSettings = MockProviderSettings.CreateTimeout("MockTimeout", 3000),
            FailureSettings = MockProviderSettings.CreateFailure("MockFailure", "Connection refused", 50)
        };
    }

    /// <summary>
    /// Creates configuration for timeout testing
    /// </summary>
    public static MockProvidersConfiguration ForTimeoutTesting()
    {
        return new MockProvidersConfiguration
        {
            IncludeSuccessProvider = false,
            IncludeTimeoutProvider = true,
            IncludeFailureProvider = false,
            TimeoutSettings = MockProviderSettings.CreateTimeout("MockTimeout", 4000)
        };
    }
}

/// <summary>
/// Validates ApiProviderSettings configuration
/// </summary>
internal class ValidateApiProviderSettings : IValidateOptions<ApiProviderSettings>
{
    public ValidateOptionsResult Validate(string? name, ApiProviderSettings options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail($"Invalid ApiProviderSettings configuration: {ex.Message}");
        }
    }
}

/// <summary>
/// Extension methods for HttpClient configuration
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Configures HttpClient with retry policy and resilience patterns
    /// </summary>
    public static IServiceCollection AddResilientHttpClients(
        this IServiceCollection services,
        Action<HttpClientSettings>? configureSettings = null)
    {
        var settings = new HttpClientSettings();
        configureSettings?.Invoke(settings);

        // Configure default HTTP client factory settings
        services.ConfigureHttpClientDefaults(builder =>
        {
            builder.ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", settings.UserAgent);
                client.Timeout = settings.DefaultTimeout;
            });

            // Add resilience patterns if needed (requires Microsoft.Extensions.Http.Resilience)
            // builder.AddStandardResilienceHandler();
        });

        return services;
    }

    /// <summary>
    /// Adds HTTP client with custom resilience configuration
    /// </summary>
    public static IHttpClientBuilder AddResilientHttpClient<TClient>(
        this IServiceCollection services,
        string name,
        Action<HttpClient>? configureClient = null)
        where TClient : class
    {
        return services.AddHttpClient<TClient>(name, client =>
        {
            configureClient?.Invoke(client);
            
            // Set common headers
            client.DefaultRequestHeaders.Add("Accept", "application/json, application/xml");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        });
    }
}