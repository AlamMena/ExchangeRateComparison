using System.ComponentModel.DataAnnotations;

namespace MockApi3.JsonProvider.DTOs;


/// <summary>
/// Nested exchange details for API3 format
/// </summary>
public class ExchangeDetails
{
    /// <summary>
    /// Source currency code (e.g., "USD")
    /// </summary>
    [Required]
    public string SourceCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Target currency code (e.g., "EUR")
    /// </summary>
    [Required]
    public string TargetCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Quantity/amount to convert
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }
}