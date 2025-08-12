using ExchangeRateComparison.Domain.Exceptions;
using System.Net;
using System.Text.Json;
using ExchangeRateComparison.WebApi.DTOs;

namespace ExchangeRateComparison.WebApi.Middleware;

/// <summary>
/// Middleware for handling exceptions and providing consistent error responses
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString();
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId ?? "Unknown",
            ["ExceptionType"] = exception.GetType().Name
        });

        _logger.LogError(exception, "Unhandled exception occurred during request processing");

        var response = CreateErrorResponse(exception, correlationId);
        
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = "application/json";

        // Add error headers
        context.Response.Headers["X-Error-Code"] = response.Code;
        if (!string.IsNullOrEmpty(correlationId))
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private ApiErrorResponse CreateErrorResponse(Exception exception, string? correlationId)
    {
        return exception switch
        {
            // Domain-specific exceptions
            ExchangeRateDomainException domainEx => new ApiErrorResponse
            {
                Code = domainEx.ErrorCode,
                Message = domainEx.Message,
                StatusCode = (int)HttpStatusCode.BadRequest,
                CorrelationId = correlationId,
                Help = GetHelpForDomainError(domainEx.ErrorCode)
            },

            // Validation exceptions
            ArgumentException argEx => ApiErrorResponse.CreateBadRequestError(
                "Invalid argument provided",
                argEx.Message,
                correlationId),

            // Timeout exceptions
            OperationCanceledException => ApiErrorResponse.CreateTimeoutError(
                "The operation was cancelled or timed out",
                correlationId),

            TimeoutException => ApiErrorResponse.CreateTimeoutError(
                "The operation timed out",
                correlationId),

            // HTTP-related exceptions
            HttpRequestException httpEx => new ApiErrorResponse
            {
                Code = "HTTP_REQUEST_ERROR",
                Message = "Failed to communicate with external service",
                Details = _environment.IsDevelopment() ? httpEx.Message : null,
                StatusCode = (int)HttpStatusCode.BadGateway,
                CorrelationId = correlationId,
                Help = "This might be a temporary issue with an external service"
            },

            // JSON serialization exceptions
            JsonException jsonEx => ApiErrorResponse.CreateBadRequestError(
                "Invalid JSON format",
                _environment.IsDevelopment() ? jsonEx.Message : "The request contains invalid JSON",
                correlationId),

            // Generic exceptions
            _ => ApiErrorResponse.CreateInternalServerError(
                _environment.IsDevelopment() ? exception.Message : null,
                correlationId)
        };
    }

    private static string GetHelpForDomainError(string errorCode)
    {
        return errorCode switch
        {
            "INVALID_CURRENCY" => "Please provide valid 3-letter ISO 4217 currency codes (e.g., USD, EUR, GBP)",
            "INVALID_AMOUNT" => "Amount must be a positive number greater than 0",
            "SAME_CURRENCIES" => "Source and target currencies must be different",
            "NO_PROVIDERS" => "No exchange rate providers are currently available. Please try again later",
            "PROVIDER_UNAVAILABLE" => "One or more exchange rate providers are temporarily unavailable",
            _ => "Please check your request parameters and try again"
        };
    }
}