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
/// HTTP provider for API3 - JSON nested format: {exchange: {sourceCurrency, targetCurrency, quantity}} → {statusCode, message, data: {total}}
/// </summary>
public class Api3JsonHttpProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Api3JsonHttpProvider> _logger;
    private readonly Api3Settings _settings;

    public string ProviderName => "API3";

    public bool IsAvailable => _settings.IsEnabled;

    public Api3JsonHttpProvider(
        HttpClient httpClient,
        ILogger<Api3JsonHttpProvider> logger,
        IOptions<ApiProviderSettings> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = options?.Value?.Api3 ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ExchangeRateOffer?> GetOfferAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("API3 provider is disabled");
            return null;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("API3: Starting request for {Request}", request.ToString());

            // Create nested request payload
            var requestDto = new Api3RequestDto
            {
                Exchange = new Api3ExchangeDto
                {
                    SourceCurrency = request.SourceCurrency,
                    TargetCurrency = request.TargetCurrency,
                    Quantity = request.Amount
                }
            };

            // Serialize request
            var requestJson = JsonSerializer.Serialize(requestDto, Api3JsonSerializerOptions.Default);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // Add API key if configured
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
            }

            _logger.LogDebug("API3: Sending POST to {Url} with payload: {Payload}", _settings.FullUrl, requestJson);

            // Make HTTP request
            using var response = await _httpClient.PostAsync(_settings.FullUrl, content, cancellationToken);

            stopwatch.Stop();

            // Parse response regardless of HTTP status (API3 may return errors in JSON)
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogDebug("API3: Received response in {Duration}ms: {Response}", 
                stopwatch.ElapsedMilliseconds, responseJson);

            var responseDto = JsonSerializer.Deserialize<Api3ResponseDto>(responseJson, Api3JsonSerializerOptions.Default);

            if (responseDto == null)
            {
                _logger.LogWarning("API3: Failed to deserialize response");
                
                return ExchangeRateOffer.CreateFailed(
                    ProviderName,
                    "Failed to parse response JSON",
                    stopwatch.Elapsed);
            }

            // Check if API3 returned an error status
            if (responseDto.StatusCode != 200)
            {
                var errorMessage = !string.IsNullOrEmpty(responseDto.Message) 
                    ? responseDto.Message 
                    : $"API returned status code: {responseDto.StatusCode}";

                _logger.LogWarning(
                    "API3: API error in {Duration}ms - Status: {StatusCode}, Message: {Message}",
                    stopwatch.ElapsedMilliseconds,
                    responseDto.StatusCode,
                    responseDto.Message);

                return ExchangeRateOffer.CreateFailed(
                    ProviderName,
                    errorMessage,
                    stopwatch.Elapsed);
            }

            // Check if we have valid data
            if (responseDto.Data?.Total == null || responseDto.Data.Total <= 0)
            {
                _logger.LogWarning("API3: Invalid total in response: {Total}", responseDto.Data?.Total);
                
                return ExchangeRateOffer.CreateFailed(
                    ProviderName,
                    "Invalid or missing total in response data",
                    stopwatch.Elapsed);
            }

            // Calculate exchange rate
            var convertedAmount = responseDto.Data.Total.Value;
            var exchangeRate = convertedAmount / request.Amount;

            var offer = ExchangeRateOffer.CreateSuccessful(
                ProviderName,
                convertedAmount,
                exchangeRate,
                stopwatch.Elapsed);

            _logger.LogInformation(
                "API3: Successfully received offer - Rate: {Rate:F4}, Amount: {ConvertedAmount:F2} in {Duration}ms",
                exchangeRate,
                convertedAmount,
                stopwatch.ElapsedMilliseconds);

            return offer;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("API3: Request timed out after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                "Request timed out",
                stopwatch.Elapsed);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "API3: HTTP request failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                $"HTTP request failed: {ex.Message}",
                stopwatch.Elapsed);
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "API3: JSON parsing failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                $"Invalid JSON response: {ex.Message}",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "API3: Unexpected error after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
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
            _logger.LogDebug("API3: Performing health check");

            // Simple GET request to base URL to check if service is available
            var healthCheckUrl = _settings.BaseUrl.TrimEnd('/') + "/health";
            
            using var response = await _httpClient.GetAsync(healthCheckUrl, cancellationToken);
            
            var isHealthy = response.IsSuccessStatusCode;
            
            _logger.LogDebug("API3: Health check result: {IsHealthy}", isHealthy);
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API3: Health check failed");
            return false;
        }
    }
}

/// <summary>
/// Request DTO for API3: {exchange: {sourceCurrency, targetCurrency, quantity}}
/// </summary>
internal record Api3RequestDto
{
    public Api3ExchangeDto Exchange { get; init; } = new();
}

/// <summary>
/// Nested exchange DTO for API3 request
/// </summary>
internal record Api3ExchangeDto
{
    public string SourceCurrency { get; init; } = string.Empty;
    public string TargetCurrency { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
}

/// <summary>
/// Response DTO for API3: {statusCode, message, data: {total}}
/// </summary>
internal record Api3ResponseDto
{
    public int StatusCode { get; init; }
    public string? Message { get; init; }
    public Api3DataDto? Data { get; init; }
}

/// <summary>
/// Nested data DTO for API3 response
/// </summary>
internal record Api3DataDto
{
    public decimal? Total { get; init; }
}

/// <summary>
/// JSON serializer options for API3
/// </summary>
internal static class Api3JsonSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };
}