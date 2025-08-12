namespace ExchangeRateComparison.WebApi.DTOs;


/// <summary>
/// Response model for provider health status
/// </summary>
public class ProviderHealthResponse
{
    public DateTime CheckedAt { get; set; }
    public int TotalProviders { get; set; }
    public int HealthyProviders { get; set; }
    public int UnhealthyProviders { get; set; }
    public List<ProviderHealthInfo> Providers { get; set; } = new();
}

/// <summary>
/// Health information for a specific provider
/// </summary>
public class ProviderHealthInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Response model for provider information
/// </summary>
public class ProvidersInfoResponse
{
    public int TotalProviders { get; set; }
    public int AvailableProviders { get; set; }
    public int UnavailableProviders { get; set; }
    public List<ProviderInfo> Providers { get; set; } = new();
}

/// <summary>
/// Information about a provider
/// </summary>
public class ProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string? Description { get; set; }
    public DateTime? LastHealthCheck { get; set; }
}

/// <summary>
/// Response model for supported currencies
/// </summary>
public class SupportedCurrenciesResponse
{
    public int TotalCurrencies { get; set; }
    public List<CurrencyInfo> Currencies { get; set; } = new();
}

/// <summary>
/// Information about a currency
/// </summary>
public class CurrencyInfo
{
    public string Code { get; set; } = string.Empty;
    public bool IsSupported { get; set; }
}