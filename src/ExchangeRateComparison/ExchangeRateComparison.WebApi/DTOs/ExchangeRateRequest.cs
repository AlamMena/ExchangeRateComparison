using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request model for currency exchange rate comparison
/// </summary>
public class ExchangeRateRequest
{
    /// <summary>
    /// Source currency code (e.g., "USD")
    /// </summary>
    [Required(ErrorMessage = "Source currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Source currency must be exactly 3 characters")]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Source currency must be 3 uppercase letters")]
    public string SourceCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Target currency code (e.g., "EUR")
    /// </summary>
    [Required(ErrorMessage = "Target currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Target currency must be exactly 3 characters")]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Target currency must be 3 uppercase letters")]
    public string TargetCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Amount to convert
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be between 0.01 and 1,000,000,000")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional timeout in seconds for the comparison process (default: use system setting)
    /// </summary>
    [Range(1, 30, ErrorMessage = "Timeout must be between 1 and 30 seconds")]
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Whether to include detailed provider information in the response
    /// </summary>
    public bool IncludeProviderDetails { get; set; } = true;

    /// <summary>
    /// Whether to include performance metrics in the response
    /// </summary>
    public bool IncludePerformanceMetrics { get; set; } = false;
}