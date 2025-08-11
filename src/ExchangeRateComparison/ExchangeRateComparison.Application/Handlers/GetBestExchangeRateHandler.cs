using ExchangeRateComparison.Application.Configuration;
using ExchangeRateComparison.Domain.Entities;
using ExchangeRateComparison.Domain.Exceptions;
using ExchangeRateComparison.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ExchangeRateComparison.Application.Handlers;

/// <summary>
/// Handles the process of getting the best exchange rate from multiple providers
/// </summary>
public class GetBestExchangeRateHandler : IGetBestExchangeRateHandler
{
    private readonly IEnumerable<IExchangeRateProvider> _providers;
    private readonly ILogger<GetBestExchangeRateHandler> _logger;
    private readonly ExchangeRateOptions _options;

    public GetBestExchangeRateHandler(
        IEnumerable<IExchangeRateProvider> providers,
        ILogger<GetBestExchangeRateHandler> logger,
        IOptions<ExchangeRateOptions> options)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // Validate configuration
        _options.Validate();
    }

    /// <summary>
    /// Executes the comparison process to find the best exchange rate
    /// </summary>
    /// <param name="request">The exchange request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The comparison result with the best offer</returns>
    public async Task<ExchangeComparisonResult> HandleAsync(
        ExchangeRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var stopwatch = Stopwatch.StartNew();
        var processedAt = DateTime.UtcNow;
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["SourceCurrency"] = request.SourceCurrency,
            ["TargetCurrency"] = request.TargetCurrency,
            ["Amount"] = request.Amount
        });

        _logger.LogInformation(
            "Starting exchange rate comparison for {ExchangeRequest}",
            request.ToString());

        try
        {
            // Get available providers
            var availableProviders = GetAvailableProviders();
            
            if (!availableProviders.Any())
            {
                _logger.LogWarning("No exchange rate providers are available");
                
                if (_options.FailFastOnAllProviderFailures)
                {
                    throw ExchangeRateDomainException.NoProvidersAvailable();
                }

                stopwatch.Stop();
                return ExchangeComparisonResult.CreateSuccessful(
                    request, 
                    Array.Empty<ExchangeRateOffer>(), 
                    stopwatch.Elapsed);
            }

            // Get offers from all available providers in parallel
            var offers = await GetOffersFromAllProvidersAsync(
                availableProviders, 
                request, 
                correlationId,
                cancellationToken);
            
            stopwatch.Stop();

            // Create the result
            var result = ExchangeComparisonResult.CreateSuccessful(
                request,
                offers,
                stopwatch.Elapsed);

            LogCompletionSummary(result, correlationId);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Exchange rate comparison was cancelled after {Duration}ms", 
                stopwatch.ElapsedMilliseconds);

            return ExchangeComparisonResult.CreateFailed(
                request,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to complete exchange rate comparison after {Duration}ms", 
                stopwatch.ElapsedMilliseconds);

            return ExchangeComparisonResult.CreateFailed(
                request,
                stopwatch.Elapsed);
        }
    }

    private List<IExchangeRateProvider> GetAvailableProviders()
    {
        var providers = _providers.ToList();
        var availableProviders = providers.Where(p => p.IsAvailable).ToList();

        _logger.LogInformation(
            "Found {TotalProviders} providers, {AvailableProviders} available: {ProviderNames}",
            providers.Count,
            availableProviders.Count,
            string.Join(", ", availableProviders.Select(p => p.ProviderName)));

        return availableProviders;
    }

    private async Task<List<ExchangeRateOffer>> GetOffersFromAllProvidersAsync(
        IEnumerable<IExchangeRateProvider> providers,
        ExchangeRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var providersList = providers.ToList();
        
        _logger.LogInformation(
            "Querying {ProviderCount} providers for exchange rates", 
            providersList.Count);

        // Create tasks for all providers with individual timeout and error handling
        var tasks = providersList.Select(provider => 
            GetOfferWithTimeoutAndErrorHandlingAsync(provider, request, correlationId, cancellationToken))
            .ToList();

        // Wait for all tasks to complete (or timeout/fail)
        var offers = await Task.WhenAll(tasks);

        // Filter out null offers and return the list
        var validOffers = offers.Where(offer => offer != null)
                                .Cast<ExchangeRateOffer>()
                                .ToList();

        _logger.LogInformation(
            "Received {ValidOffers} valid offers from {TotalProviders} providers",
            validOffers.Count,
            providersList.Count);

        return validOffers;
    }

    private async Task<ExchangeRateOffer?> GetOfferWithTimeoutAndErrorHandlingAsync(
        IExchangeRateProvider provider,
        ExchangeRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var providerStopwatch = Stopwatch.StartNew();
        
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.ProviderTimeout);

            _logger.LogDebug(
                "Requesting offer from provider: {ProviderName} (Timeout: {Timeout}ms)",
                provider.ProviderName, 
                _options.ProviderTimeout.TotalMilliseconds);

            var offer = await provider.GetOfferAsync(request, timeoutCts.Token);
            providerStopwatch.Stop();

            if (offer != null)
            {
                LogProviderResult(provider.ProviderName, offer, providerStopwatch.Elapsed, correlationId);
                return offer;
            }

            _logger.LogWarning(
                "Provider {ProviderName} returned null offer after {Duration}ms",
                provider.ProviderName, 
                providerStopwatch.ElapsedMilliseconds);

            return ExchangeRateOffer.CreateFailed(
                provider.ProviderName, 
                "Provider returned null offer",
                providerStopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            providerStopwatch.Stop();
            _logger.LogWarning(
                "Request was cancelled for provider: {ProviderName} after {Duration}ms",
                provider.ProviderName, 
                providerStopwatch.ElapsedMilliseconds);

            return ExchangeRateOffer.CreateFailed(
                provider.ProviderName, 
                "Request was cancelled",
                providerStopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            providerStopwatch.Stop();
            _logger.LogWarning(
                "Request timed out for provider: {ProviderName} after {Duration}ms (Timeout: {TimeoutMs}ms)",
                provider.ProviderName, 
                providerStopwatch.ElapsedMilliseconds,
                _options.ProviderTimeout.TotalMilliseconds);

            return ExchangeRateOffer.CreateFailed(
                provider.ProviderName, 
                $"Request timed out after {_options.ProviderTimeout.TotalSeconds}s",
                providerStopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            providerStopwatch.Stop();
            _logger.LogError(ex, 
                "Error getting offer from provider: {ProviderName} after {Duration}ms",
                provider.ProviderName, 
                providerStopwatch.ElapsedMilliseconds);

            return ExchangeRateOffer.CreateFailed(
                provider.ProviderName, 
                ex.Message,
                providerStopwatch.Elapsed);
        }
    }

    private void LogProviderResult(
        string providerName, 
        ExchangeRateOffer offer, 
        TimeSpan duration,
        string correlationId)
    {
        if (offer.IsSuccessful)
        {
            if (_options.EnablePerformanceLogging)
            {
                _logger.LogInformation(
                    "✅ {ProviderName}: {ConvertedAmount:F2} (Rate: {ExchangeRate:F4}) in {Duration}ms [CorrelationId: {CorrelationId}]",
                    providerName, 
                    offer.ConvertedAmount, 
                    offer.ExchangeRate,
                    duration.TotalMilliseconds,
                    correlationId);
            }
            else
            {
                _logger.LogInformation(
                    "✅ {ProviderName}: {ConvertedAmount:F2} (Rate: {ExchangeRate:F4})",
                    providerName, 
                    offer.ConvertedAmount, 
                    offer.ExchangeRate);
            }
        }
        else
        {
            _logger.LogWarning(
                "❌ {ProviderName}: Failed - {ErrorMessage} in {Duration}ms [CorrelationId: {CorrelationId}]",
                providerName, 
                offer.ErrorMessage,
                duration.TotalMilliseconds,
                correlationId);
        }
    }

    private void LogCompletionSummary(ExchangeComparisonResult result, string correlationId)
    {
        var summary = new
        {
            Status = result.Status.ToString(),
            BestProvider = result.BestOffer?.ProviderName,
            BestAmount = result.BestOffer?.ConvertedAmount,
            BestRate = result.BestOffer?.ExchangeRate,
            SuccessfulOffers = result.SuccessfulOffersCount,
            FailedOffers = result.FailedOffersCount,
            TotalProviders = result.AllOffers.Count,
            ProcessingTime = $"{result.ProcessingDuration.TotalMilliseconds:F1}ms",
            Savings = result.CalculateSavings(),
            SavingsPercentage = $"{result.CalculateSavingsPercentage():F2}%"
        };

        if (result.BestOffer != null)
        {
            _logger.LogInformation(
                "🎯 Exchange rate comparison completed successfully! " +
                "Best offer: {BestProvider} = {BestAmount:F2} (Rate: {BestRate:F4}) | " +
                "Results: {SuccessfulOffers}✅/{FailedOffers}❌ | " +
                "Processing time: {ProcessingTime} | " +
                "Savings: {Savings:F2} ({SavingsPercentage}) | " +
                "[CorrelationId: {CorrelationId}]",
                summary.BestProvider,
                summary.BestAmount,
                summary.BestRate,
                summary.SuccessfulOffers,
                summary.FailedOffers,
                summary.ProcessingTime,
                summary.Savings,
                summary.SavingsPercentage,
                correlationId);
        }
        else
        {
            _logger.LogWarning(
                "⚠️ Exchange rate comparison completed with no successful offers. " +
                "Results: {SuccessfulOffers}✅/{FailedOffers}❌ | " +
                "Processing time: {ProcessingTime} | " +
                "[CorrelationId: {CorrelationId}]",
                summary.SuccessfulOffers,
                summary.FailedOffers,
                summary.ProcessingTime,
                correlationId);
        }
    }
}