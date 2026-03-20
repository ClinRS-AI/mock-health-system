using System.Net;
using System.Text.Json;
using MockHealthSystem.Api.Models;

namespace MockHealthSystem.Api.Middleware;

/// <summary>
/// Global exception handling middleware. Logs exceptions and returns a consistent API error response.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, ex);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, title, detail) = exception switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "Bad Request", exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Not Found", exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An error occurred", _environment.IsDevelopment() ? exception.Message : null)
        };

        context.Response.StatusCode = (int)statusCode;

        var traceId = context.TraceIdentifier;
        var response = new ApiErrorResponse
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            TraceId = traceId
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
