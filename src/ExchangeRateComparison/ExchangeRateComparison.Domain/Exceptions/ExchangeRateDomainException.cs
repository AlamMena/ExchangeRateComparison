namespace ExchangeRateComparison.Domain.Exceptions;

/// <summary>
/// Exception thrown when domain business rules are violated
/// </summary>
public class ExchangeRateDomainException : Exception
{
    public string ErrorCode { get; }

    public ExchangeRateDomainException(string errorCode, string message) 
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public ExchangeRateDomainException(string errorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates an exception for invalid currency codes
    /// </summary>
    public static ExchangeRateDomainException InvalidCurrency(string currencyCode)
    {
        return new ExchangeRateDomainException(
            "INVALID_CURRENCY", 
            $"Invalid currency code '{currencyCode}'. Must be a 3-letter ISO 4217 code.");
    }

    /// <summary>
    /// Creates an exception for invalid amounts
    /// </summary>
    public static ExchangeRateDomainException InvalidAmount(decimal amount)
    {
        return new ExchangeRateDomainException(
            "INVALID_AMOUNT", 
            $"Invalid amount '{amount}'. Amount must be greater than zero.");
    }

    /// <summary>
    /// Creates an exception for same source and target currencies
    /// </summary>
    public static ExchangeRateDomainException SameCurrencies(string currency)
    {
        return new ExchangeRateDomainException(
            "SAME_CURRENCIES", 
            $"Source and target currencies cannot be the same: '{currency}'.");
    }

    /// <summary>
    /// Creates an exception for provider unavailability
    /// </summary>
    public static ExchangeRateDomainException ProviderUnavailable(string providerName)
    {
        return new ExchangeRateDomainException(
            "PROVIDER_UNAVAILABLE", 
            $"Exchange rate provider '{providerName}' is currently unavailable.");
    }

    /// <summary>
    /// Creates an exception for no available providers
    /// </summary>
    public static ExchangeRateDomainException NoProvidersAvailable()
    {
        return new ExchangeRateDomainException(
            "NO_PROVIDERS", 
            "No exchange rate providers are currently available.");
    }
}