namespace ExchangeRateComparison.Domain.Entities;

/// <summary>
/// Represents a currency exchange request containing the source currency, target currency, and amount
/// </summary>
public record ExchangeRequest
{
    public string SourceCurrency { get; init; }
    public string TargetCurrency { get; init; }
    public decimal Amount { get; init; }

    public ExchangeRequest(string sourceCurrency, string targetCurrency, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(sourceCurrency))
            throw new ArgumentException("Source currency cannot be null or empty", nameof(sourceCurrency));
        
        if (string.IsNullOrWhiteSpace(targetCurrency))
            throw new ArgumentException("Target currency cannot be null or empty", nameof(targetCurrency));
        
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (sourceCurrency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Source and target currencies cannot be the same", nameof(targetCurrency));

        SourceCurrency = sourceCurrency.ToUpperInvariant().Trim();
        TargetCurrency = targetCurrency.ToUpperInvariant().Trim();
        Amount = amount;
    }

    /// <summary>
    /// Validates if the currency code follows ISO 4217 format (3 letters)
    /// </summary>
    private static bool IsValidCurrencyCode(string currencyCode)
    {
        return !string.IsNullOrWhiteSpace(currencyCode) 
               && currencyCode.Length == 3 
               && currencyCode.All(char.IsLetter);
    }

    /// <summary>
    /// Creates an ExchangeRequest with additional currency code validation
    /// </summary>
    public static ExchangeRequest CreateWithValidation(string sourceCurrency, string targetCurrency, decimal amount)
    {
        if (!IsValidCurrencyCode(sourceCurrency))
            throw new ArgumentException($"Invalid source currency code: {sourceCurrency}. Must be 3 letters (ISO 4217)", nameof(sourceCurrency));
        
        if (!IsValidCurrencyCode(targetCurrency))
            throw new ArgumentException($"Invalid target currency code: {targetCurrency}. Must be 3 letters (ISO 4217)", nameof(targetCurrency));

        return new ExchangeRequest(sourceCurrency, targetCurrency, amount);
    }

    /// <summary>
    /// Returns a string representation of the exchange request
    /// </summary>
    public override string ToString()
    {
        return $"{Amount:F2} {SourceCurrency} → {TargetCurrency}";
    }
}