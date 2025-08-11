namespace ExchangeRateComparison.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for all API providers
/// </summary>
public class ApiProviderSettings
{
    public const string SectionName = "ApiProviders";

    /// <summary>
    /// Settings for API1 (JSON provider)
    /// </summary>
    public Api1Settings Api1 { get; set; } = new();

    /// <summary>
    /// Settings for API2 (XML provider)
    /// </summary>
    public Api2Settings Api2 { get; set; } = new();

    /// <summary>
    /// Settings for API3 (JSON nested provider)
    /// </summary>
    public Api3Settings Api3 { get; set; } = new();

    /// <summary>
    /// Global HTTP client settings
    /// </summary>
    public HttpClientSettings HttpClient { get; set; } = new();

    /// <summary>
    /// Validates all provider settings
    /// </summary>
    public void Validate()
    {
        Api1.Validate();
        Api2.Validate();
        Api3.Validate();
        HttpClient.Validate();
    }
}

/// <summary>
/// Settings for API1 JSON provider
/// </summary>
public class Api1Settings
{
    public string BaseUrl { get; set; } = "http://localhost:5001";
    public string Endpoint { get; set; } = "/exchange";
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 3;

    public string FullUrl => $"{BaseUrl.TrimEnd('/')}{Endpoint}";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("Api1 BaseUrl cannot be empty");
        
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("Api1 Endpoint cannot be empty");

        if (TimeoutSeconds <= 0)
            throw new InvalidOperationException("Api1 TimeoutSeconds must be greater than 0");
    }
}

/// <summary>
/// Settings for API2 XML provider
/// </summary>
public class Api2Settings
{
    public string BaseUrl { get; set; } = "http://localhost:5002";
    public string Endpoint { get; set; } = "/convert";
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 3;

    public string FullUrl => $"{BaseUrl.TrimEnd('/')}{Endpoint}";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("Api2 BaseUrl cannot be empty");
        
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("Api2 Endpoint cannot be empty");

        if (TimeoutSeconds <= 0)
            throw new InvalidOperationException("Api2 TimeoutSeconds must be greater than 0");
    }
}

/// <summary>
/// Settings for API3 JSON nested provider
/// </summary>
public class Api3Settings
{
    public string BaseUrl { get; set; } = "http://localhost:5003";
    public string Endpoint { get; set; } = "/currency-exchange";
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 3;

    public string FullUrl => $"{BaseUrl.TrimEnd('/')}{Endpoint}";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("Api3 BaseUrl cannot be empty");
        
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("Api3 Endpoint cannot be empty");

        if (TimeoutSeconds <= 0)
            throw new InvalidOperationException("Api3 TimeoutSeconds must be greater than 0");
    }
}

/// <summary>
/// Global HTTP client settings
/// </summary>
public class HttpClientSettings
{
    public int DefaultTimeoutSeconds { get; set; } = 5;
    public int MaxRetryAttempts { get; set; } = 2;
    public int RetryDelayMilliseconds { get; set; } = 500;
    public string UserAgent { get; set; } = "ExchangeRateComparison/1.0";
    public bool EnableDetailedLogging { get; set; } = true;

    public TimeSpan DefaultTimeout => TimeSpan.FromSeconds(DefaultTimeoutSeconds);
    public TimeSpan RetryDelay => TimeSpan.FromMilliseconds(RetryDelayMilliseconds);

    public void Validate()
    {
        if (DefaultTimeoutSeconds <= 0)
            throw new InvalidOperationException("DefaultTimeoutSeconds must be greater than 0");

        if (MaxRetryAttempts < 0)
            throw new InvalidOperationException("MaxRetryAttempts must be greater than or equal to 0");

        if (RetryDelayMilliseconds < 0)
            throw new InvalidOperationException("RetryDelayMilliseconds must be greater than or equal to 0");

        if (string.IsNullOrWhiteSpace(UserAgent))
            throw new InvalidOperationException("UserAgent cannot be empty");
    }
}