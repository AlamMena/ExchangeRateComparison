using ExchangeRateComparison.Application.Handlers;
using ExchangeRateComparison.Domain.Entities;
using ExchangeRateComparison.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExchangeRateComparison.Application.Services;

/// <summary>
/// Implementation of exchange rate service
/// </summary>
public class ExchangeRateService(
    GetBestExchangeRateHandler handler,
    IEnumerable<IExchangeRateProvider> providers,
    ILogger<ExchangeRateService> logger)
    : IExchangeRateService
{
    private readonly GetBestExchangeRateHandler _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    private readonly IEnumerable<IExchangeRateProvider> _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    private readonly ILogger<ExchangeRateService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Compares exchange rates from multiple providers and returns the best offer
    /// </summary>
    public async Task<ExchangeComparisonResult> CompareExchangeRatesAsync(
        ExchangeRequest request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug(
            "Starting exchange rate comparison service for {Request}",
            request.ToString());

        try
        {
            var result = await _handler.HandleAsync(request, cancellationToken);
            
            _logger.LogDebug(
                "Exchange rate comparison service completed with status: {Status}",
                result.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Exchange rate comparison service failed for request: {Request}",
                request.ToString());
            throw;
        }
    }

    /// <summary>
    /// Gets the health status of all registered exchange rate providers
    /// </summary>
    public async Task<Dictionary<string, bool>> GetProvidersHealthAsync(CancellationToken cancellationToken = default)
    {
        var providers = _providers.ToList();
        var healthResults = new Dictionary<string, bool>();

        _logger.LogDebug("Checking health status of {ProviderCount} providers", providers.Count);

        // Check all providers in parallel
        var healthTasks = providers.Select(async provider =>
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout for health checks

                var isHealthy = await provider.HealthCheckAsync(timeoutCts.Token);
                
                _logger.LogDebug(
                    "Provider {ProviderName} health check: {Status}",
                    provider.ProviderName,
                    isHealthy ? "Healthy" : "Unhealthy");

                return new { Provider = provider.ProviderName, IsHealthy = isHealthy };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Health check timed out for provider: {ProviderName}",
                    provider.ProviderName);
                return new { Provider = provider.ProviderName, IsHealthy = false };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Health check failed for provider: {ProviderName}",
                    provider.ProviderName);
                return new { Provider = provider.ProviderName, IsHealthy = false };
            }
        }).ToArray();

        var results = await Task.WhenAll(healthTasks);

        foreach (var result in results)
        {
            healthResults[result.Provider] = result.IsHealthy;
        }

        _logger.LogInformation(
            "Health check completed for {TotalProviders} providers. Healthy: {HealthyCount}, Unhealthy: {UnhealthyCount}",
            healthResults.Count,
            healthResults.Values.Count(h => h),
            healthResults.Values.Count(h => !h));

        return healthResults;
    }

    /// <summary>
    /// Gets information about all registered providers
    /// </summary>
    public async Task<IEnumerable<ProviderInfo>> GetAvailableProvidersAsync()
    {
        var providers = _providers.ToList();
        
        _logger.LogDebug("Getting information for {ProviderCount} providers", providers.Count);

        var providerInfos = new List<ProviderInfo>();

        foreach (var provider in providers)
        {
            var info = new ProviderInfo(
                Name: provider.ProviderName,
                IsAvailable: provider.IsAvailable,
                LastHealthCheck: DateTime.UtcNow,
                Description: $"Exchange rate provider: {provider.ProviderName}");

            providerInfos.Add(info);
        }

        _logger.LogDebug(
            "Retrieved information for {TotalProviders} providers. Available: {AvailableCount}",
            providerInfos.Count,
            providerInfos.Count(p => p.IsAvailable));

        return providerInfos;
    }
}