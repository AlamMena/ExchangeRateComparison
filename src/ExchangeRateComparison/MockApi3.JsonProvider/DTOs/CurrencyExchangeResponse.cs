namespace MockApi3.JsonProvider.DTOs;


/// <summary>
/// Response model for currency exchange - API3 format: {statusCode, message, data: {total}}
/// </summary>
public class CurrencyExchangeResponse
{
    /// <summary>
    /// HTTP status code (200 for success, 400+ for errors)
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response message ("success" for successful operations)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Response data containing the conversion result
    /// </summary>
    public CurrencyExchangeData? Data { get; set; }
}