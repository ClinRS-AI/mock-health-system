using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private const int BodyMaxLength = 4096;

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly string? _frontendOrigin;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        var frontendUrl = configuration["FRONTEND_URL"];
        if (!string.IsNullOrWhiteSpace(frontendUrl))
        {
            try
            {
                var uri = new Uri(frontendUrl);
                _frontendOrigin = uri.GetLeftPart(UriPartial.Authority);
            }
            catch
            {
                _frontendOrigin = frontendUrl;
            }
        }
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        // Skip logging for UI-originated requests based on Origin/Referer.
        var origin = context.Request.Headers.Origin.ToString();
        var referer = context.Request.Headers.Referer.ToString();

        if (!string.IsNullOrWhiteSpace(_frontendOrigin))
        {
            if ((!string.IsNullOrEmpty(origin) && origin.StartsWith(_frontendOrigin, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(referer) && referer.StartsWith(_frontendOrigin, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }
        }

        var request = context.Request;
        var response = context.Response;

        var startedAt = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        string? requestBody = null;

        if (request.ContentLength > 0 && request.Body.CanRead)
        {
            try
            {
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var body = await reader.ReadToEndAsync(context.RequestAborted);
                requestBody = Truncate(body, BodyMaxLength);
                request.Body.Position = 0;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read request body for logging.");
            }
        }

        var originalBodyStream = response.Body;
        await using var responseBody = new MemoryStream();
        response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            string? responseText = null;
            try
            {
                responseBody.Position = 0;
                using var reader = new StreamReader(responseBody, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var text = await reader.ReadToEndAsync(context.RequestAborted);
                responseText = Truncate(text, BodyMaxLength);
                responseBody.Position = 0;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read response body for logging.");
            }

            await responseBody.CopyToAsync(originalBodyStream, context.RequestAborted);
            response.Body = originalBodyStream;

            try
            {
                var log = new ApiRequestLog
                {
                    CreatedAtUtc = startedAt,
                    Method = request.Method,
                    Path = request.Path.ToString(),
                    QueryString = request.QueryString.HasValue ? request.QueryString.Value : null,
                    StatusCode = response.StatusCode,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    Origin = origin,
                    Referer = referer,
                    UserAgent = request.Headers.UserAgent.ToString(),
                    RemoteIp = context.Connection.RemoteIpAddress?.ToString(),
                    RequestBody = requestBody,
                    ResponseBody = responseText,
                    CorrelationId = context.TraceIdentifier
                };

                dbContext.ApiRequestLogs.Add(log);
                await dbContext.SaveChangesAsync(context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist API request log.");
            }
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}

