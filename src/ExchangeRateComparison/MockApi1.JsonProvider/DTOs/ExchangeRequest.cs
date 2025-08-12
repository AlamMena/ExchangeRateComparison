using System.ComponentModel.DataAnnotations;

namespace MockApi1.JsonProvider.DTOs;

/// <summary>
/// Request model for currency exchange - API1 format: {from, to, value}
/// </summary>
public class ExchangeRequest
{
    /// <summary>
    /// Source currency code (e.g., "USD")
    /// </summary>
    [Required]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Target currency code (e.g., "EUR")
    /// </summary>
    [Required]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Amount to convert
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Value must be greater than 0")]
    public decimal Value { get; set; }
}