using System.ComponentModel.DataAnnotations;

namespace MockApi3.JsonProvider.DTOs;

/// <summary>
/// Request model for currency exchange - API3 format: {exchange: {sourceCurrency, targetCurrency, quantity}}
/// </summary>
public class CurrencyExchangeRequest
{
    /// <summary>
    /// Nested exchange object containing the currency exchange details
    /// </summary>
    [Required]
    public ExchangeDetails? Exchange { get; set; }
}
