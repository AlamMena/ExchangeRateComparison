using ExchangeRateComparison.Domain.Entities;
using ExchangeRateComparison.Domain.Interfaces;
using ExchangeRateComparison.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ExchangeRateComparison.Infrastructure.Providers;

/// <summary>
/// HTTP provider for API1 - JSON format: {from, to, value} → {rate}
/// </summary>
public class Api1JsonHttpProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Api1JsonHttpProvider> _logger;
    private readonly Api1Settings _settings;

    public string ProviderName => "API1";

    public bool IsAvailable => _settings.IsEnabled;

    public Api1JsonHttpProvider(
        HttpClient httpClient,
        ILogger<Api1JsonHttpProvider> logger,
        IOptions<ApiProviderSettings> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = options?.Value?.Api1 ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ExchangeRateOffer?> GetOfferAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("API1 provider is disabled");
            return null;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("API1: Starting request for {Request}", request.ToString());

            // Create request payload
            var requestDto = new Api1RequestDto
            {
                From = request.SourceCurrency,
                To = request.TargetCurrency,
                Value = request.Amount
            };

            // Serialize request
            var requestJson = JsonSerializer.Serialize(requestDto, Api1JsonSerializerOptions.Default);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // Add API key if configured
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
            }

            _logger.LogDebug("API1: Sending POST to {Url} with payload: {Payload}", _settings.FullUrl, requestJson);

            // Make HTTP request
            using var response = await _httpClient.PostAsync(_settings.FullUrl, content, cancellationToken);

            stopwatch.Stop();

            // Check if request was successful
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                _logger.LogWarning(
                    "API1: HTTP {StatusCode} response in {Duration}ms: {Error}",
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    errorContent);

                return ExchangeRateOffer.CreateFailed(
                    ProviderName,
                    $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    stopwatch.Elapsed);
            }

            // Parse response
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogDebug("API1: Received response in {Duration}ms: {Response}", 
                stopwatch.ElapsedMilliseconds, responseJson);

            var responseDto = JsonSerializer.Deserialize<Api1ResponseDto>(responseJson, Api1JsonSerializerOptions.Default);

            if (responseDto?.Rate == null || responseDto.Rate <= 0)
            {
                _logger.LogWarning("API1: Invalid rate in response: {Rate}", responseDto?.Rate);
                
                return ExchangeRateOffer.CreateFailed(
                    ProviderName,
                    "Invalid or missing rate in response",
                    stopwatch.Elapsed);
            }

            // Calculate converted amount
            var convertedAmount = request.Amount * responseDto.Rate.Value;

            var offer = ExchangeRateOffer.CreateSuccessful(
                ProviderName,
                convertedAmount,
                responseDto.Rate.Value,
                stopwatch.Elapsed);

            _logger.LogInformation(
                "API1: Successfully received offer - Rate: {Rate:F4}, Amount: {ConvertedAmount:F2} in {Duration}ms",
                responseDto.Rate.Value,
                convertedAmount,
                stopwatch.ElapsedMilliseconds);

            return offer;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("API1: Request timed out after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                "Request timed out",
                stopwatch.Elapsed);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "API1: HTTP request failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                $"HTTP request failed: {ex.Message}",
                stopwatch.Elapsed);
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "API1: JSON parsing failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                $"Invalid JSON response: {ex.Message}",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "API1: Unexpected error after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                $"Unexpected error: {ex.Message}",
                stopwatch.Elapsed);
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            return false;

        try
        {
            _logger.LogDebug("API1: Performing health check");

            // Simple GET request to base URL to check if service is available
            var healthCheckUrl = _settings.BaseUrl.TrimEnd('/') + "/health";
            
            using var response = await _httpClient.GetAsync(healthCheckUrl, cancellationToken);
            
            var isHealthy = response.IsSuccessStatusCode;
            
            _logger.LogDebug("API1: Health check result: {IsHealthy}", isHealthy);
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API1: Health check failed");
            return false;
        }
    }
}

/// <summary>
/// Request DTO for API1: {from, to, value}
/// </summary>
internal record Api1RequestDto
{
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public decimal Value { get; init; }
}

/// <summary>
/// Response DTO for API1: {rate}
/// </summary>
internal record Api1ResponseDto
{
    public decimal? Rate { get; init; }
}

/// <summary>
/// JSON serializer options for API1
/// </summary>
internal static class Api1JsonSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };
}