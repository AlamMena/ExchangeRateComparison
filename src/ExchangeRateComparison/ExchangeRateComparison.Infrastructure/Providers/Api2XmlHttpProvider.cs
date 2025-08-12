using ExchangeRateComparison.Domain.Entities;
using ExchangeRateComparison.Domain.Interfaces;
using ExchangeRateComparison.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace ExchangeRateComparison.Infrastructure.Providers;

/// <summary>
/// HTTP provider for API2 - XML format: &lt;XML&gt;&lt;From/&gt;&lt;To/&gt;&lt;Amount/&gt;&lt;/XML&gt; → &lt;XML&gt;&lt;Result/&gt;&lt;/XML&gt;
/// </summary>
public class Api2XmlHttpProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Api2XmlHttpProvider> _logger;
    private readonly Api2Settings _settings;

    public string ProviderName => "API2";

    public bool IsAvailable => _settings.IsEnabled;

    public Api2XmlHttpProvider(
        HttpClient httpClient,
        ILogger<Api2XmlHttpProvider> logger,
        IOptions<ApiProviderSettings> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = options?.Value?.Api2 ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ExchangeRateOffer?> GetOfferAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("API2 provider is disabled");
            return null;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("API2: Starting request for {Request}", request.ToString());

            // Create XML request payload
            var requestXml = CreateRequestXml(request);

            var content = new StringContent(requestXml, Encoding.UTF8, "application/xml");

            // Add API key if configured
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
            }

            _logger.LogDebug("API2: Sending POST to {Url} with XML payload: {Payload}", _settings.FullUrl, requestXml);

            // Make HTTP request
            using var response = await _httpClient.PostAsync(_settings.FullUrl, content, cancellationToken);

            stopwatch.Stop();

            // Check if request was successful
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                _logger.LogWarning(
                    "API2: HTTP {StatusCode} response in {Duration}ms: {Error}",
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    errorContent);

                return ExchangeRateOffer.CreateFailed(
                    ProviderName,
                    $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    stopwatch.Elapsed);
            }

            // Parse response
            var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogDebug("API2: Received response in {Duration}ms: {Response}", 
                stopwatch.ElapsedMilliseconds, responseXml);

            var convertedAmount = ParseResponseXml(responseXml);

            if (convertedAmount <= 0)
            {
                _logger.LogWarning("API2: Invalid converted amount in response: {Amount}", convertedAmount);
                
                return ExchangeRateOffer.CreateFailed(
                    ProviderName,
                    "Invalid or missing result in XML response",
                    stopwatch.Elapsed);
            }

            // Calculate exchange rate
            var exchangeRate = convertedAmount / request.Amount;

            var offer = ExchangeRateOffer.CreateSuccessful(
                ProviderName,
                convertedAmount,
                exchangeRate,
                stopwatch.Elapsed);

            _logger.LogInformation(
                "API2: Successfully received offer - Rate: {Rate:F4}, Amount: {ConvertedAmount:F2} in {Duration}ms",
                exchangeRate,
                convertedAmount,
                stopwatch.ElapsedMilliseconds);

            return offer;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("API2: Request timed out after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                "Request timed out",
                stopwatch.Elapsed);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "API2: HTTP request failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                $"HTTP request failed: {ex.Message}",
                stopwatch.Elapsed);
        }
        catch (Exception ex) when (ex is XmlException || ex is FormatException || ex is InvalidOperationException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "API2: XML parsing failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return ExchangeRateOffer.CreateFailed(
                ProviderName,
                $"Invalid XML response: {ex.Message}",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "API2: Unexpected error after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
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
            _logger.LogDebug("API2: Performing health check");

            // Simple GET request to base URL to check if service is available
            var healthCheckUrl = _settings.BaseUrl.TrimEnd('/') + "/health";
            
            using var response = await _httpClient.GetAsync(healthCheckUrl, cancellationToken);
            
            var isHealthy = response.IsSuccessStatusCode;
            
            _logger.LogDebug("API2: Health check result: {IsHealthy}", isHealthy);
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API2: Health check failed");
            return false;
        }
    }

    /// <summary>
    /// Creates XML request payload: &lt;XML&gt;&lt;From&gt;...&lt;/From&gt;&lt;To&gt;...&lt;/To&gt;&lt;Amount&gt;...&lt;/Amount&gt;&lt;/XML&gt;
    /// </summary>
    private string CreateRequestXml(ExchangeRequest request)
    {
        var xml = new XElement("XML",
            new XElement("From", request.SourceCurrency),
            new XElement("To", request.TargetCurrency),
            new XElement("Amount", request.Amount.ToString("F2", CultureInfo.InvariantCulture))
        );

        return xml.ToString();
    }

    /// <summary>
    /// Parses XML response: &lt;XML&gt;&lt;Result&gt;...&lt;/Result&gt;&lt;/XML&gt;
    /// </summary>
    private decimal ParseResponseXml(string responseXml)
    {
        try
        {
            var xml = XDocument.Parse(responseXml);
            var resultElement = xml.Root?.Element("Result");

            if (resultElement == null)
            {
                throw new InvalidOperationException("Missing Result element in XML response");
            }

            var resultValue = resultElement.Value;

            if (string.IsNullOrWhiteSpace(resultValue))
            {
                throw new InvalidOperationException("Empty Result element in XML response");
            }

            if (!decimal.TryParse(resultValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            {
                throw new FormatException($"Invalid decimal format in Result element: {resultValue}");
            }

            return result;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException || ex is FormatException))
        {
            throw new XmlException($"Failed to parse XML response: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Custom XML exception for API2
/// </summary>
internal class XmlException : Exception
{
    public XmlException(string message) : base(message) { }
    public XmlException(string message, Exception innerException) : base(message, innerException) { }
}