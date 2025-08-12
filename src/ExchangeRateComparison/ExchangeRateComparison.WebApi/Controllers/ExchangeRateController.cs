using ExchangeRateComparison.Application.Services;
using ExchangeRateComparison.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using ExchangeRateComparison.WebApi.DTOs;
using ProviderInfo = ExchangeRateComparison.WebApi.DTOs.ProviderInfo;

namespace ExchangeRateComparison.WebApi.Controllers;

/// <summary>
/// Controller for exchange rate comparison operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/exchange-rates")]
public class ExchangeRateController(
    IExchangeRateService exchangeRateService,
    ILogger<ExchangeRateController> logger)
    : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
    private readonly ILogger<ExchangeRateController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Compare exchange rates from multiple providers and return the best offer
    /// </summary>
    /// <param name="request">Exchange rate comparison request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comparison result with the best offer and all provider responses</returns>
    /// <response code="200">Comparison completed successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="408">Request timeout</response>
    /// <response code="500">Internal server error</response>
    /// <response code="503">Service unavailable</response>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(ExchangeRateResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 408)]
    [ProducesResponseType(typeof(ApiErrorResponse), 500)]
    [ProducesResponseType(typeof(ApiErrorResponse), 503)]
    public async Task<IActionResult> CompareExchangeRates(
        [FromBody] ExchangeRateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exchange rate comparison requested: {SourceCurrency} to {TargetCurrency}, Amount: {Amount}",
                request.SourceCurrency, request.TargetCurrency, request.Amount);

            // Convert API request to domain request
            var domainRequest = ExchangeRequest.CreateWithValidation(
                request.SourceCurrency,
                request.TargetCurrency,
                request.Amount);

            // Apply timeout if specified
            using var timeoutCts = request.TimeoutSeconds.HasValue
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : null;

            if (timeoutCts != null)
            {
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds.Value));
                cancellationToken = timeoutCts.Token;
            }

            // Execute comparison
            var result = await _exchangeRateService.CompareExchangeRatesAsync(domainRequest, timeoutCts.Token);

            _logger.LogInformation("Exchange rate comparison completed: Status={Status}, BestOffer={BestOffer}, Duration={Duration}",
                result.Status, result.BestOffer?.ConvertedAmount, result.ProcessingDuration);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid exchange rate request parameters");
            
            var errorResponse = ApiErrorResponse.CreateBadRequestError(
                "Invalid request parameters", 
                ex.Message);
            
            return BadRequest(errorResponse);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Exchange rate comparison request was cancelled or timed out");
            
            var errorResponse = ApiErrorResponse.CreateTimeoutError(
                "The exchange rate comparison request timed out");
            
            return StatusCode(408, errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during exchange rate comparison");
            
            var errorResponse = ApiErrorResponse.CreateInternalServerError(
                "An unexpected error occurred while comparing exchange rates");
            
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Get health status of all exchange rate providers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status of all providers</returns>
    /// <response code="200">Provider health status successfully retrieved</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("providers/health")]
    [ProducesResponseType(typeof(ProviderHealthResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 500)]
    public async Task<IActionResult> GetProvidersHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Provider health check requested");

            var healthStatus = await _exchangeRateService.GetProvidersHealthAsync(cancellationToken);

            var response = new ProviderHealthResponse
            {
                CheckedAt = DateTime.UtcNow,
                TotalProviders = healthStatus.Count,
                HealthyProviders = healthStatus.Values.Count(h => h),
                UnhealthyProviders = healthStatus.Values.Count(h => !h),
                Providers = healthStatus.Select(kvp => new ProviderHealthInfo
                {
                    Name = kvp.Key,
                    IsHealthy = kvp.Value,
                    Status = kvp.Value ? "Healthy" : "Unhealthy"
                }).ToList()
            };

            _logger.LogInformation("Provider health check completed: {HealthyCount}/{TotalCount} providers healthy",
                response.HealthyProviders, response.TotalProviders);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking provider health status");
            
            var errorResponse = ApiErrorResponse.CreateInternalServerError(
                "Failed to check provider health status");
            
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Get information about available exchange rate providers
    /// </summary>
    /// <returns>List of available providers with their details</returns>
    /// <response code="200">Provider information successfully retrieved</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(ProvidersInfoResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 500)]
    public async Task<IActionResult> GetAvailableProviders()
    {
        try
        {
            _logger.LogDebug("Available providers information requested");

            var providers = await _exchangeRateService.GetAvailableProvidersAsync();

            var providerInfos = providers as Application.Services.ProviderInfo[] ?? providers.ToArray();
            var response = new ProvidersInfoResponse
            {
                TotalProviders = providerInfos.Length,
                AvailableProviders = providerInfos.Count(p => p.IsAvailable),
                UnavailableProviders = providerInfos.Count(p => !p.IsAvailable),
                Providers = providerInfos.Select(p => new ProviderInfo
                {
                    Name = p.Name,
                    IsAvailable = p.IsAvailable,
                    Description = p.Description,
                    LastHealthCheck = p.LastHealthCheck
                }).ToList()
            };

            _logger.LogDebug("Provider information retrieved: {AvailableCount}/{TotalCount} providers available",
                response.AvailableProviders, response.TotalProviders);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provider information");
            
            var errorResponse = ApiErrorResponse.CreateInternalServerError(
                "Failed to retrieve provider information");
            
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Get supported currency codes
    /// </summary>
    /// <returns>List of supported currency codes</returns>
    /// <response code="200">Supported currencies retrieved successfully</response>
    [HttpGet("currencies")]
    [ProducesResponseType(typeof(SupportedCurrenciesResponse), 200)]
    public IActionResult GetSupportedCurrencies()
    {
        try
        {
            _logger.LogDebug("Supported currencies requested");

            // This would typically come from configuration or a service
            // For now, return a common set of currencies
            var currencies = new[]
            {
                "USD", "EUR", "GBP", "JPY", "AUD", "CAD", "CHF", "CNY", 
                "SEK", "NZD", "MXN", "SGD", "HKD", "NOK", "TRY", "RUB",
                "INR", "BRL", "ZAR", "KRW", "DOP", "COP", "CLP", "PEN",
                "ARS", "UYU", "VES", "BOB", "PYG", "GTQ"
            };

            var response = new SupportedCurrenciesResponse
            {
                TotalCurrencies = currencies.Length,
                Currencies = currencies.Select(code => new CurrencyInfo
                {
                    Code = code,
                    IsSupported = true
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supported currencies");
            return StatusCode(500, "Failed to retrieve supported currencies");
        }
    }
}
