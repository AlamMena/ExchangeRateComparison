namespace ExchangeRateComparison.WebApi.DTOs;

/// <summary>
/// Standardized API error response
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error description
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Additional context or suggestions
    /// </summary>
    public string? Help { get; set; }

    /// <summary>
    /// Field-specific validation errors (for 400 Bad Request)
    /// </summary>
    public Dictionary<string, List<string>>? ValidationErrors { get; set; }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static ApiErrorResponse CreateValidationError(Dictionary<string, List<string>> validationErrors, string? correlationId = null)
    {
        return new ApiErrorResponse
        {
            Code = "VALIDATION_ERROR",
            Message = "One or more validation errors occurred",
            Details = "Please check the request format and required fields",
            StatusCode = 400,
            CorrelationId = correlationId,
            ValidationErrors = validationErrors,
            Help = "Ensure all required fields are provided with valid values"
        };
    }

    /// <summary>
    /// Creates a not found error response
    /// </summary>
    public static ApiErrorResponse CreateNotFoundError(string resource, string? correlationId = null)
    {
        return new ApiErrorResponse
        {
            Code = "NOT_FOUND",
            Message = $"The requested {resource} was not found",
            StatusCode = 404,
            CorrelationId = correlationId,
            Help = "Check the URL and ensure the resource exists"
        };
    }

    /// <summary>
    /// Creates an internal server error response
    /// </summary>
    public static ApiErrorResponse CreateInternalServerError(string? details = null, string? correlationId = null)
    {
        return new ApiErrorResponse
        {
            Code = "INTERNAL_SERVER_ERROR",
            Message = "An unexpected error occurred while processing your request",
            Details = details,
            StatusCode = 500,
            CorrelationId = correlationId,
            Help = "Please try again later or contact support if the problem persists"
        };
    }

    /// <summary>
    /// Creates a service unavailable error response
    /// </summary>
    public static ApiErrorResponse CreateServiceUnavailableError(string? details = null, string? correlationId = null)
    {
        return new ApiErrorResponse
        {
            Code = "SERVICE_UNAVAILABLE",
            Message = "The service is temporarily unavailable",
            Details = details ?? "One or more exchange rate providers are currently unavailable",
            StatusCode = 503,
            CorrelationId = correlationId,
            Help = "Please try again in a few moments"
        };
    }

    /// <summary>
    /// Creates a timeout error response
    /// </summary>
    public static ApiErrorResponse CreateTimeoutError(string? details = null, string? correlationId = null)
    {
        return new ApiErrorResponse
        {
            Code = "REQUEST_TIMEOUT",
            Message = "The request timed out",
            Details = details ?? "The exchange rate comparison took longer than expected",
            StatusCode = 408,
            CorrelationId = correlationId,
            Help = "Try reducing the timeout or check provider availability"
        };
    }

    /// <summary>
    /// Creates a rate limit error response
    /// </summary>
    public static ApiErrorResponse CreateRateLimitError(string? details = null, string? correlationId = null)
    {
        return new ApiErrorResponse
        {
            Code = "RATE_LIMIT_EXCEEDED",
            Message = "Rate limit exceeded",
            Details = details ?? "Too many requests in a short period",
            StatusCode = 429,
            CorrelationId = correlationId,
            Help = "Please wait before making additional requests"
        };
    }

    /// <summary>
    /// Creates a bad request error response
    /// </summary>
    public static ApiErrorResponse CreateBadRequestError(string message, string? details = null, string? correlationId = null)
    {
        return new ApiErrorResponse
        {
            Code = "BAD_REQUEST",
            Message = message,
            Details = details,
            StatusCode = 400,
            CorrelationId = correlationId,
            Help = "Please check the request format and parameters"
        };
    }
}