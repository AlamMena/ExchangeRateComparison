namespace ExchangeRateComparison.Domain.Enums;

/// <summary>
/// Represents the status of the exchange rate comparison process
/// </summary>
public enum ProcessStatus
{
    /// <summary>
    /// The process is currently running and querying providers
    /// </summary>
    Processing,
    
    /// <summary>
    /// The process has completed successfully (with or without successful offers)
    /// </summary>
    Completed,
    
    /// <summary>
    /// The process failed due to an unexpected error
    /// </summary>
    Failed
}