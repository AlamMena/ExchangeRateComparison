using ExchangeRateComparison.Domain.Entities;
using ExchangeRateComparison.Domain.Exceptions;

namespace ExchangeRateComparison.Application.Common;

/// <summary>
/// Helper methods for input validation in the application layer
/// </summary>
public static class ValidationHelpers
{
    /// <summary>
    /// Common currency codes supported by most exchange rate providers
    /// </summary>
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP", "JPY", "AUD", "CAD", "CHF", "CNY", "SEK", "NZD",
        "MXN", "SGD", "HKD", "NOK", "TRY", "RUB", "INR", "BRL", "ZAR", "KRW",
        "DOP", "COP", "CLP", "PEN", "ARS", "UYU", "VES", "BOB", "PYG", "GTQ"
    };

    /// <summary>
    /// Maximum allowed amount for exchange rate requests
    /// </summary>
    public const decimal MaxAllowedAmount = 1_000_000_000m; // 1 billion

    /// <summary>
    /// Minimum allowed amount for exchange rate requests
    /// </summary>
    public const decimal MinAllowedAmount = 0.01m;

    /// <summary>
    /// Validates an exchange request and throws detailed exceptions for any issues
    /// </summary>
    /// <param name="request">The exchange request to validate</param>
    /// <exception cref="ArgumentNullException">When request is null</exception>
    /// <exception cref="ExchangeRateDomainException">When validation fails</exception>
    public static void ValidateExchangeRequest(ExchangeRequest? request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Validate currencies
        ValidateCurrency(request.SourceCurrency, nameof(request.SourceCurrency));
        ValidateCurrency(request.TargetCurrency, nameof(request.TargetCurrency));

        // Check if currencies are different
        if (string.Equals(request.SourceCurrency, request.TargetCurrency, StringComparison.OrdinalIgnoreCase))
        {
            throw ExchangeRateDomainException.SameCurrencies(request.SourceCurrency);
        }

        // Validate amount
        ValidateAmount(request.Amount);
    }

    /// <summary>
    /// Validates a currency code
    /// </summary>
    /// <param name="currencyCode">The currency code to validate</param>
    /// <param name="parameterName">The parameter name for exception messages</param>
    /// <exception cref="ExchangeRateDomainException">When currency is invalid</exception>
    public static void ValidateCurrency(string currencyCode, string parameterName = "currency")
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException($"Currency code cannot be null or empty", parameterName);
        }

        // Check format (3 letters)
        if (currencyCode.Length != 3 || !currencyCode.All(char.IsLetter))
        {
            throw ExchangeRateDomainException.InvalidCurrency(currencyCode);
        }

        // Check if currency is supported (optional - can be commented out for broader support)
        if (!SupportedCurrencies.Contains(currencyCode))
        {
            // Log warning but don't throw - allow unsupported currencies to pass through
            // Some providers might support currencies not in our list
        }
    }

    /// <summary>
    /// Validates an amount for exchange rate requests
    /// </summary>
    /// <param name="amount">The amount to validate</param>
    /// <exception cref="ExchangeRateDomainException">When amount is invalid</exception>
    public static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw ExchangeRateDomainException.InvalidAmount(amount);
        }

        if (amount < MinAllowedAmount)
        {
            throw new ExchangeRateDomainException(
                "AMOUNT_TOO_SMALL",
                $"Amount {amount} is too small. Minimum allowed amount is {MinAllowedAmount}");
        }

        if (amount > MaxAllowedAmount)
        {
            throw new ExchangeRateDomainException(
                "AMOUNT_TOO_LARGE",
                $"Amount {amount} is too large. Maximum allowed amount is {MaxAllowedAmount:N0}");
        }
    }

    /// <summary>
    /// Checks if a currency is commonly supported by exchange rate providers
    /// </summary>
    /// <param name="currencyCode">The currency code to check</param>
    /// <returns>True if the currency is commonly supported</returns>
    public static bool IsCommonlySupportedCurrency(string currencyCode)
    {
        return SupportedCurrencies.Contains(currencyCode);
    }

    /// <summary>
    /// Gets all commonly supported currency codes
    /// </summary>
    /// <returns>List of supported currency codes</returns>
    public static IReadOnlyList<string> GetSupportedCurrencies()
    {
        return SupportedCurrencies.OrderBy(c => c).ToList().AsReadOnly();
    }

    /// <summary>
    /// Normalizes a currency code (trims and converts to uppercase)
    /// </summary>
    /// <param name="currencyCode">The currency code to normalize</param>
    /// <returns>Normalized currency code</returns>
    public static string NormalizeCurrency(string currencyCode)
    {
        return currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Validates that a collection of providers is not empty
    /// </summary>
    /// <param name="providers">The providers to validate</param>
    /// <exception cref="ExchangeRateDomainException">When no providers are available</exception>
    public static void ValidateProvidersAvailable(IEnumerable<object> providers)
    {
        if (providers == null || !providers.Any())
        {
            throw ExchangeRateDomainException.NoProvidersAvailable();
        }
    }

    /// <summary>
    /// Creates a validation summary for an exchange request
    /// </summary>
    /// <param name="request">The request to validate</param>
    /// <returns>Validation result with details</returns>
    public static ValidationResult ValidateExchangeRequestWithResult(ExchangeRequest? request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("Request cannot be null");
            return new ValidationResult(false, errors);
        }

        // Validate source currency
        try
        {
            ValidateCurrency(request.SourceCurrency, nameof(request.SourceCurrency));
        }
        catch (Exception ex)
        {
            errors.Add($"Source currency: {ex.Message}");
        }

        // Validate target currency  
        try
        {
            ValidateCurrency(request.TargetCurrency, nameof(request.TargetCurrency));
        }
        catch (Exception ex)
        {
            errors.Add($"Target currency: {ex.Message}");
        }

        // Check same currencies
        if (string.Equals(request.SourceCurrency, request.TargetCurrency, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Source and target currencies cannot be the same");
        }

        // Validate amount
        try
        {
            ValidateAmount(request.Amount);
        }
        catch (Exception ex)
        {
            errors.Add($"Amount: {ex.Message}");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}

/// <summary>
/// Result of a validation operation
/// </summary>
/// <param name="IsValid">Whether the validation passed</param>
/// <param name="Errors">List of validation errors</param>
public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Gets the first error message, if any
    /// </summary>
    public string? FirstError => Errors.FirstOrDefault();

    /// <summary>
    /// Gets all error messages as a single string
    /// </summary>
    public string ErrorMessage => string.Join("; ", Errors);
}