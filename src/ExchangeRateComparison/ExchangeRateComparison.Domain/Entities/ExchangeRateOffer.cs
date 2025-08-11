namespace ExchangeRateComparison.Domain.Entities;

/// <summary>
/// Represents an exchange rate offer from a specific provider
/// </summary>
public record ExchangeRateOffer
{
    public string ProviderName { get; init; }
    public decimal ConvertedAmount { get; init; }
    public decimal ExchangeRate { get; init; }
    public DateTime ResponseTime { get; init; }
    public bool IsSuccessful { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ResponseDuration { get; init; }

    public ExchangeRateOffer(
        string providerName, 
        decimal convertedAmount, 
        decimal exchangeRate, 
        DateTime responseTime,
        bool isSuccessful = true,
        string? errorMessage = null,
        TimeSpan responsesDuration = default)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));

        if (isSuccessful)
        {
            if (convertedAmount < 0)
                throw new ArgumentException("Converted amount cannot be negative for successful offers", nameof(convertedAmount));
            
            if (exchangeRate <= 0)
                throw new ArgumentException("Exchange rate must be positive for successful offers", nameof(exchangeRate));
        }

        ProviderName = providerName;
        ConvertedAmount = convertedAmount;
        ExchangeRate = exchangeRate;
        ResponseTime = responseTime;
        IsSuccessful = isSuccessful;
        ErrorMessage = errorMessage;
        ResponseDuration = responsesDuration;
    }

    /// <summary>
    /// Creates a successful exchange rate offer
    /// </summary>
    public static ExchangeRateOffer CreateSuccessful(
        string providerName, 
        decimal convertedAmount, 
        decimal exchangeRate,
        TimeSpan responseDuration = default)
    {
        return new ExchangeRateOffer(
            providerName, 
            convertedAmount, 
            exchangeRate, 
            DateTime.UtcNow, 
            true, 
            null,
            responseDuration);
    }

    /// <summary>
    /// Creates a failed exchange rate offer for error scenarios
    /// </summary>
    public static ExchangeRateOffer CreateFailed(
        string providerName, 
        string errorMessage,
        TimeSpan responseDuration = default)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty for failed offers", nameof(errorMessage));

        return new ExchangeRateOffer(
            providerName, 
            0, 
            0, 
            DateTime.UtcNow, 
            false, 
            errorMessage,
            responseDuration);
    }

    /// <summary>
    /// Calculates the original amount from converted amount and exchange rate
    /// </summary>
    public decimal CalculateOriginalAmount()
    {
        return IsSuccessful && ExchangeRate > 0 ? ConvertedAmount / ExchangeRate : 0;
    }

    /// <summary>
    /// Determines if this offer is better than another (higher converted amount)
    /// </summary>
    public bool IsBetterThan(ExchangeRateOffer? other)
    {
        if (other == null || !other.IsSuccessful)
            return IsSuccessful;
        
        if (!IsSuccessful)
            return false;

        return ConvertedAmount > other.ConvertedAmount;
    }

    /// <summary>
    /// Returns a string representation of the offer
    /// </summary>
    public override string ToString()
    {
        return IsSuccessful 
            ? $"{ProviderName}: {ConvertedAmount:F2} (Rate: {ExchangeRate:F4})"
            : $"{ProviderName}: Failed - {ErrorMessage}";
    }
}