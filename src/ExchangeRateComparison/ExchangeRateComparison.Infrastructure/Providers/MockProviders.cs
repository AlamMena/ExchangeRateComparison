using ExchangeRateComparison.Domain.Entities;
using ExchangeRateComparison.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ExchangeRateComparison.Infrastructure.Providers.MockProviders;

/// <summary>
/// Mock provider that always returns a successful exchange rate offer
/// </summary>
public class MockSuccessProvider : IExchangeRateProvider
{
    private readonly ILogger<MockSuccessProvider> _logger;
    private readonly MockProviderSettings _settings;

    public string ProviderName => _settings.ProviderName;

    public bool IsAvailable => _settings.IsEnabled;

    public MockSuccessProvider(
        ILogger<MockSuccessProvider> logger,
        MockProviderSettings? settings = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? MockProviderSettings.CreateSuccess("MockSuccess");
    }

    public async Task<ExchangeRateOffer?> GetOfferAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("{ProviderName} provider is disabled", ProviderName);
            return null;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("{ProviderName}: Starting mock request for {Request}", ProviderName, request.ToString());

            // Simulate processing delay
            if (_settings.DelayMs > 0)
            {
                await Task.Delay(_settings.DelayMs, cancellationToken);
            }

            stopwatch.Stop();

            // Generate predictable but realistic exchange rate
            var exchangeRate = CalculateMockExchangeRate(request.SourceCurrency, request.TargetCurrency);
            var convertedAmount = request.Amount * exchangeRate;

            var offer = ExchangeRateOffer.CreateSuccessful(
                ProviderName,
                convertedAmount,
                exchangeRate,
                stopwatch.Elapsed);

            _logger.LogInformation(
                "{ProviderName}: Successfully returned mock offer - Rate: {Rate:F4}, Amount: {ConvertedAmount:F2} in {Duration}ms",
                ProviderName,
                exchangeRate,
                convertedAmount,
                stopwatch.ElapsedMilliseconds);

            return offer;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("{ProviderName}: Mock request was cancelled after {Duration}ms", 
                ProviderName, stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                "Request was cancelled",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "{ProviderName}: Mock request failed after {Duration}ms", 
                ProviderName, stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                $"Mock error: {ex.Message}",
                stopwatch.Elapsed);
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            return false;

        try
        {
            _logger.LogDebug("{ProviderName}: Performing mock health check", ProviderName);

            // Simulate health check delay
            if (_settings.DelayMs > 0)
            {
                await Task.Delay(Math.Min(_settings.DelayMs, 100), cancellationToken);
            }

            // Mock providers are always healthy if enabled
            _logger.LogDebug("{ProviderName}: Mock health check passed", ProviderName);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{ProviderName}: Mock health check was cancelled", ProviderName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ProviderName}: Mock health check failed", ProviderName);
            return false;
        }
    }

    /// <summary>
    /// Calculates a mock exchange rate based on currency pair
    /// </summary>
    private static decimal CalculateMockExchangeRate(string sourceCurrency, string targetCurrency)
    {
        // Generate deterministic but realistic rates based on currency pair
        var hashCode = (sourceCurrency + targetCurrency).GetHashCode();
        var seed = Math.Abs(hashCode % 1000);

        return sourceCurrency.ToUpperInvariant() switch
        {
            "USD" when targetCurrency.ToUpperInvariant() == "EUR" => 0.85m + (seed * 0.0001m),
            "USD" when targetCurrency.ToUpperInvariant() == "GBP" => 0.75m + (seed * 0.0001m),
            "USD" when targetCurrency.ToUpperInvariant() == "DOP" => 55.50m + (seed * 0.01m),
            "EUR" when targetCurrency.ToUpperInvariant() == "USD" => 1.18m + (seed * 0.0001m),
            "EUR" when targetCurrency.ToUpperInvariant() == "GBP" => 0.88m + (seed * 0.0001m),
            "GBP" when targetCurrency.ToUpperInvariant() == "USD" => 1.33m + (seed * 0.0001m),
            "GBP" when targetCurrency.ToUpperInvariant() == "EUR" => 1.14m + (seed * 0.0001m),
            "DOP" when targetCurrency.ToUpperInvariant() == "USD" => 0.018m + (seed * 0.000001m),
            _ => 1.0m + (seed * 0.0001m) // Default rate with small variation
        };
    }
}

/// <summary>
/// Mock provider that always times out
/// </summary>
public class MockTimeoutProvider : IExchangeRateProvider
{
    private readonly ILogger<MockTimeoutProvider> _logger;
    private readonly MockProviderSettings _settings;

    public string ProviderName => _settings.ProviderName;

    public bool IsAvailable => _settings.IsEnabled;

    public MockTimeoutProvider(
        ILogger<MockTimeoutProvider> logger,
        MockProviderSettings? settings = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? MockProviderSettings.CreateTimeout("MockTimeout", 5000); // 5 second timeout
    }

    public async Task<ExchangeRateOffer?> GetOfferAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("{ProviderName} provider is disabled", ProviderName);
            return null;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("{ProviderName}: Starting mock timeout request for {Request}", ProviderName, request.ToString());

            // Simulate a long delay that will cause timeout
            await Task.Delay(_settings.DelayMs, cancellationToken);

            // This should never execute due to timeout
            stopwatch.Stop();
            
            _logger.LogWarning("{ProviderName}: Unexpectedly completed without timeout after {Duration}ms", 
                ProviderName, stopwatch.ElapsedMilliseconds);

            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                "Unexpected completion - should have timed out",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogInformation("{ProviderName}: Mock request timed out as expected after {Duration}ms", 
                ProviderName, stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                "Request timed out (simulated)",
                stopwatch.Elapsed);
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            return false;

        try
        {
            _logger.LogDebug("{ProviderName}: Performing mock health check (will timeout)", ProviderName);

            // Simulate timeout in health check too
            await Task.Delay(_settings.DelayMs, cancellationToken);
            
            return false; // Should not reach here
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("{ProviderName}: Mock health check timed out as expected", ProviderName);
            return false;
        }
    }
}

/// <summary>
/// Mock provider that always fails with an error
/// </summary>
public class MockFailureProvider : IExchangeRateProvider
{
    private readonly ILogger<MockFailureProvider> _logger;
    private readonly MockProviderSettings _settings;

    public string ProviderName => _settings.ProviderName;

    public bool IsAvailable => _settings.IsEnabled;

    public MockFailureProvider(
        ILogger<MockFailureProvider> logger,
        MockProviderSettings? settings = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? MockProviderSettings.CreateFailure("MockFailure", "Simulated provider failure");
    }

    public async Task<ExchangeRateOffer?> GetOfferAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("{ProviderName} provider is disabled", ProviderName);
            return null;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("{ProviderName}: Starting mock failure request for {Request}", ProviderName, request.ToString());

            // Simulate processing delay before failure
            if (_settings.DelayMs > 0)
            {
                await Task.Delay(_settings.DelayMs, cancellationToken);
            }

            stopwatch.Stop();

            // Always return a failed offer
            var offer = ExchangeRateOffer.CreateFailed(
                ProviderName,
                _settings.ErrorMessage ?? "Simulated provider failure",
                stopwatch.Elapsed);

            _logger.LogInformation(
                "{ProviderName}: Returned mock failure as expected in {Duration}ms: {ErrorMessage}",
                ProviderName,
                stopwatch.ElapsedMilliseconds,
                offer.ErrorMessage);

            return offer;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("{ProviderName}: Mock failure request was cancelled after {Duration}ms", 
                ProviderName, stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                "Request was cancelled",
                stopwatch.Elapsed);
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            return false;

        try
        {
            _logger.LogDebug("{ProviderName}: Performing mock health check (will fail)", ProviderName);

            // Simulate delay before health check failure
            if (_settings.DelayMs > 0)
            {
                await Task.Delay(Math.Min(_settings.DelayMs, 500), cancellationToken);
            }

            _logger.LogDebug("{ProviderName}: Mock health check failed as expected", ProviderName);
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{ProviderName}: Mock health check was cancelled", ProviderName);
            return false;
        }
    }
}

/// <summary>
/// Configuration settings for mock providers
/// </summary>
public class MockProviderSettings
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int DelayMs { get; set; } = 100;
    public string? ErrorMessage { get; set; }

    public static MockProviderSettings CreateSuccess(string name, int delayMs = 100)
    {
        return new MockProviderSettings
        {
            ProviderName = name,
            IsEnabled = true,
            DelayMs = delayMs
        };
    }

    public static MockProviderSettings CreateTimeout(string name, int timeoutMs = 5000)
    {
        return new MockProviderSettings
        {
            ProviderName = name,
            IsEnabled = true,
            DelayMs = timeoutMs
        };
    }

    public static MockProviderSettings CreateFailure(string name, string errorMessage, int delayMs = 100)
    {
        return new MockProviderSettings
        {
            ProviderName = name,
            IsEnabled = true,
            DelayMs = delayMs,
            ErrorMessage = errorMessage
        };
    }

    public static MockProviderSettings CreateDisabled(string name)
    {
        return new MockProviderSettings
        {
            ProviderName = name,
            IsEnabled = false,
            DelayMs = 0
        };
    }
}