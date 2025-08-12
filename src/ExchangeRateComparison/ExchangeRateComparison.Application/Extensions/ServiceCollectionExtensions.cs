using ExchangeRateComparison.Application.Configuration;
using ExchangeRateComparison.Application.Handlers;
using ExchangeRateComparison.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExchangeRateComparison.Application.Extensions;

/// <summary>
/// Extension methods for registering application layer services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application layer services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<ExchangeRateOptions>(
            configuration.GetSection(ExchangeRateOptions.SectionName));

        // Validate configuration on startup
        services.AddSingleton<IValidateOptions<ExchangeRateOptions>, ValidateExchangeRateOptions>();

        // Register handlers
        services.AddScoped<GetBestExchangeRateHandler>();

        // Register application services
        services.AddScoped<IExchangeRateService, ExchangeRateService>();

        return services;
    }

    /// <summary>
    /// Adds application layer services with custom options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        Action<ExchangeRateOptions> configureOptions)
    {
        // Register configuration
        services.Configure(configureOptions);

        // Validate configuration on startup
        services.AddSingleton<IValidateOptions<ExchangeRateOptions>, ValidateExchangeRateOptions>();

        // Register handlers
        services.AddScoped<IGetBestExchangeRateHandler, GetBestExchangeRateHandler>();

        // Register application services
        services.AddScoped<IExchangeRateService, ExchangeRateService>();

        return services;
    }
}

/// <summary>
/// Validates ExchangeRateOptions configuration
/// </summary>
internal class ValidateExchangeRateOptions : IValidateOptions<ExchangeRateOptions>
{
    public ValidateOptionsResult Validate(string? name, ExchangeRateOptions options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail($"Invalid ExchangeRateOptions configuration: {ex.Message}");
        }
    }
}