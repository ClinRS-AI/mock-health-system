using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MockHealthSystem.Api.Middleware;
using MockHealthSystem.Infrastructure.Data;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

/// <summary>
/// Unit tests for RequestLoggingMiddleware that exercise the resilience and configuration branches
/// not covered by the integration suite.
/// </summary>
public sealed class RequestLoggingMiddlewareTests
{
    private static IConfiguration CreateConfig(string? frontendUrl)
    {
        var data = new Dictionary<string, string?>();
        if (frontendUrl is not null)
        {
            data["FRONTEND_URL"] = frontendUrl;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static HttpContext CreateContext(
        string method = "GET",
        string path = "/api/v1/health",
        string? queryString = null,
        string? requestBody = null,
        string? origin = null,
        string? referer = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Request.Path = path;
        if (queryString is not null)
        {
            ctx.Request.QueryString = new QueryString(queryString);
        }

        if (requestBody is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(requestBody);
            ctx.Request.Body = new MemoryStream(bytes);
            ctx.Request.ContentLength = bytes.Length;
        }

        if (origin is not null)
        {
            ctx.Request.Headers.Origin = origin;
        }

        if (referer is not null)
        {
            ctx.Request.Headers.Referer = referer;
        }

        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task Invoke_LogsRequest_WhenNoFrontendOriginConfigured()
    {
        await using var db = CreateDb(nameof(Invoke_LogsRequest_WhenNoFrontendOriginConfigured));

        var middleware = new RequestLoggingMiddleware(
            next: ctx =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestLoggingMiddleware>.Instance,
            configuration: CreateConfig(null));

        var ctx = CreateContext();
        await middleware.InvokeAsync(ctx, db);

        Assert.Equal(1, await db.ApiRequestLogs.CountAsync());
    }

    [Fact]
    public async Task Invoke_SkipsLogging_WhenOriginMatchesFrontend()
    {
        await using var db = CreateDb(nameof(Invoke_SkipsLogging_WhenOriginMatchesFrontend));

        var middleware = new RequestLoggingMiddleware(
            next: ctx =>
            {
                ctx.Response.StatusCode = 204;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestLoggingMiddleware>.Instance,
            configuration: CreateConfig("http://localhost:5176"));

        var ctx = CreateContext(origin: "http://localhost:5176");
        await middleware.InvokeAsync(ctx, db);

        Assert.Equal(0, await db.ApiRequestLogs.CountAsync());
    }

    [Fact]
    public async Task Invoke_SkipsLogging_WhenRefererMatchesFrontend()
    {
        await using var db = CreateDb(nameof(Invoke_SkipsLogging_WhenRefererMatchesFrontend));

        var middleware = new RequestLoggingMiddleware(
            next: ctx =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestLoggingMiddleware>.Instance,
            configuration: CreateConfig("http://localhost:5176"));

        var ctx = CreateContext(referer: "http://localhost:5176/admin");
        await middleware.InvokeAsync(ctx, db);

        Assert.Equal(0, await db.ApiRequestLogs.CountAsync());
    }

    [Fact]
    public async Task Invoke_LogsRequest_WhenInvalidFrontendUrlConfigured()
    {
        // Invalid URL should fall through to the catch and still be parsed as an opaque string,
        // not block logging of unrelated traffic.
        await using var db = CreateDb(nameof(Invoke_LogsRequest_WhenInvalidFrontendUrlConfigured));

        var middleware = new RequestLoggingMiddleware(
            next: ctx =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestLoggingMiddleware>.Instance,
            configuration: CreateConfig("not a valid url"));

        var ctx = CreateContext(origin: "http://different-origin");
        await middleware.InvokeAsync(ctx, db);

        Assert.Equal(1, await db.ApiRequestLogs.CountAsync());
    }

    [Fact]
    public async Task Invoke_CapturesRequestBody_WhenContentLengthIsSet()
    {
        await using var db = CreateDb(nameof(Invoke_CapturesRequestBody_WhenContentLengthIsSet));

        var middleware = new RequestLoggingMiddleware(
            next: ctx =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestLoggingMiddleware>.Instance,
            configuration: CreateConfig(null));

        var ctx = CreateContext(method: "POST", requestBody: "{\"hello\":\"world\"}");
        await middleware.InvokeAsync(ctx, db);

        var log = await db.ApiRequestLogs.SingleAsync();
        Assert.Equal("{\"hello\":\"world\"}", log.RequestBody);
    }

    [Fact]
    public async Task Invoke_TruncatesRequestBody_WhenLargerThan4096Chars()
    {
        await using var db = CreateDb(nameof(Invoke_TruncatesRequestBody_WhenLargerThan4096Chars));

        var middleware = new RequestLoggingMiddleware(
            next: ctx =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestLoggingMiddleware>.Instance,
            configuration: CreateConfig(null));

        var bigBody = new string('x', 5000);
        var ctx = CreateContext(method: "POST", requestBody: bigBody);
        await middleware.InvokeAsync(ctx, db);

        var log = await db.ApiRequestLogs.SingleAsync();
        Assert.Equal(4096, log.RequestBody!.Length);
    }

    [Fact]
    public async Task Invoke_CopiesResponseBodyToOriginalStream()
    {
        await using var db = CreateDb(nameof(Invoke_CopiesResponseBodyToOriginalStream));

        var middleware = new RequestLoggingMiddleware(
            next: async ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "text/plain";
                var bytes = Encoding.UTF8.GetBytes("hello-response");
                await ctx.Response.Body.WriteAsync(bytes);
            },
            logger: NullLogger<RequestLoggingMiddleware>.Instance,
            configuration: CreateConfig(null));

        var ctx = CreateContext();
        var originalBody = new MemoryStream();
        ctx.Response.Body = originalBody;

        await middleware.InvokeAsync(ctx, db);

        originalBody.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(originalBody).ReadToEndAsync();
        Assert.Equal("hello-response", text);

        var log = await db.ApiRequestLogs.SingleAsync();
        Assert.Equal("hello-response", log.ResponseBody);
    }

    [Fact]
    public async Task Invoke_SwallowsDbSaveError_AndStillReturnsResponse()
    {
        // Disposing the DbContext makes SaveChangesAsync throw inside the middleware, which should
        // be caught and logged without re-throwing.
        var db = CreateDb(nameof(Invoke_SwallowsDbSaveError_AndStillReturnsResponse));
        await db.DisposeAsync();

        var middleware = new RequestLoggingMiddleware(
            next: ctx =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestLoggingMiddleware>.Instance,
            configuration: CreateConfig(null));

        var ctx = CreateContext();
        var ex = await Record.ExceptionAsync(() => middleware.InvokeAsync(ctx, db));
        Assert.Null(ex);
        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_PopulatesPathAndQueryString_OnLog()
    {
        await using var db = CreateDb(nameof(Invoke_PopulatesPathAndQueryString_OnLog));

        var middleware = new RequestLoggingMiddleware(
            next: ctx =>
            {
                ctx.Response.StatusCode = 201;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestLoggingMiddleware>.Instance,
            configuration: CreateConfig(null));

        var ctx = CreateContext(path: "/api/v1/widgets", queryString: "?id=42");
        await middleware.InvokeAsync(ctx, db);

        var log = await db.ApiRequestLogs.SingleAsync();
        Assert.Equal("/api/v1/widgets", log.Path);
        Assert.Equal("?id=42", log.QueryString);
        Assert.Equal(201, log.StatusCode);
    }
}
