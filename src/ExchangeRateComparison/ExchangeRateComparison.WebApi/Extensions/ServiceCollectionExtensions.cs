using ExchangeRateComparison.WebApi.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace ExchangeRateComparison.WebApi.Extensions;

/// <summary>
/// Extension methods for registering Web API services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Web API specific services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        // Configure API behavior options
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // Customize model validation error responses
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    );

                var correlationId = context.HttpContext.GetCorrelationId();
                var errorResponse = DTOs.ApiErrorResponse.CreateValidationError(errors, correlationId);

                return new BadRequestObjectResult(errorResponse);
            };
        });

        // Add API versioning
        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ApiVersionReader = ApiVersionReader.Combine(
                new HeaderApiVersionReader("X-Version"),
                new QueryStringApiVersionReader("version"),
                new UrlSegmentApiVersionReader()
            );
        });

        // Configure Swagger generation
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Exchange Rate Comparison API",
                Description = "A REST API for comparing currency exchange rates from multiple providers",
                Contact = new OpenApiContact
                {
                    Name = "Banreservas Development Team",
                    Email = "dev@banreservas.com"
                },
                License = new OpenApiLicense
                {
                    Name = "Proprietary",
                    Url = new Uri("https://banreservas.com/terms")
                }
            });

            // Include XML comments for API documentation
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add security definition for API key authentication
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key needed to access the endpoints. Add 'X-API-Key' header with your API key.",
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme"
            });

            // Add bearer token authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            // Configure examples and schemas
            options.UseInlineDefinitionsForEnums();
            
        });

        // Configure performance options
        services.Configure<PerformanceOptions>(options =>
        {
            options.SlowRequestThresholdMs = 2000; // 2 seconds for exchange rate comparisons
            options.LogAllRequests = false;
            options.AddPerformanceHeaders = true;
            options.EnableMetricsCollection = true;
        });

        return services;
    }
}
