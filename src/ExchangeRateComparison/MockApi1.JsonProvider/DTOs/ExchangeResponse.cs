namespace MockApi1.JsonProvider.DTOs;

/// <summary>
/// Response model for currency exchange - API1 format: {rate}
/// </summary>
public class ExchangeResponse
{
    /// <summary>
    /// Exchange rate for the currency pair
    /// </summary>
    public decimal Rate { get; set; }
}