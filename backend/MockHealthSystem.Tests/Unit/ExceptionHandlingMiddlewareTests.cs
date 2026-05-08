using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using MockHealthSystem.Api.Middleware;
using MockHealthSystem.Api.Models;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class ExceptionHandlingMiddlewareTests
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static async Task<(int StatusCode, ApiErrorResponse? Body)> InvokeAsync(
        Exception exceptionToThrow,
        string environmentName = "Production")
    {
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        var env = new FakeHostEnvironment { EnvironmentName = environmentName };
        var middleware = new ExceptionHandlingMiddleware(
            next: _ => throw exceptionToThrow,
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance,
            environment: env);

        await middleware.InvokeAsync(context);

        body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(body, Encoding.UTF8).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiErrorResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return (context.Response.StatusCode, response);
    }

    [Fact]
    public async Task ArgumentException_Returns400WithBadRequestTitle()
    {
        var (statusCode, body) = await InvokeAsync(new ArgumentException("bad input"));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        Assert.Equal(400, body!.Status);
        Assert.Equal("Bad Request", body.Title);
    }

    [Fact]
    public async Task ArgumentException_IncludesExceptionMessage()
    {
        var (_, body) = await InvokeAsync(new ArgumentException("bad input"));

        Assert.Equal("bad input", body!.Detail);
    }

    [Fact]
    public async Task KeyNotFoundException_Returns404WithNotFoundTitle()
    {
        var (statusCode, body) = await InvokeAsync(new KeyNotFoundException("item missing"));

        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
        Assert.Equal(404, body!.Status);
        Assert.Equal("Not Found", body.Title);
        Assert.Equal("item missing", body.Detail);
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns401WithUnauthorizedTitle()
    {
        var (statusCode, body) = await InvokeAsync(new UnauthorizedAccessException("no access"));

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
        Assert.Equal(401, body!.Status);
        Assert.Equal("Unauthorized", body.Title);
        Assert.Equal("no access", body.Detail);
    }

    [Fact]
    public async Task GenericException_Returns500WithGenericTitle()
    {
        var (statusCode, body) = await InvokeAsync(new InvalidOperationException("internal problem"));

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.Equal(500, body!.Status);
        Assert.Equal("An error occurred", body.Title);
    }

    [Fact]
    public async Task GenericException_IncludesMessageInDetail_InDevelopment()
    {
        var (_, body) = await InvokeAsync(
            new InvalidOperationException("internal problem"),
            environmentName: "Development");

        Assert.Equal("internal problem", body!.Detail);
    }

    [Fact]
    public async Task GenericException_OmitsMessageFromDetail_InProduction()
    {
        var (_, body) = await InvokeAsync(
            new InvalidOperationException("sensitive detail"),
            environmentName: "Production");

        Assert.Null(body!.Detail);
    }

    [Fact]
    public async Task Response_HasJsonContentType()
    {
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        var middleware = new ExceptionHandlingMiddleware(
            next: _ => throw new ArgumentException("test"),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance,
            environment: new FakeHostEnvironment());

        await middleware.InvokeAsync(context);

        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task Response_PopulatesTraceId()
    {
        var (_, body) = await InvokeAsync(new ArgumentException("x"));

        Assert.NotNull(body!.TraceId);
    }

    [Fact]
    public async Task NextDelegate_IsCalledNormally_WhenNoException()
    {
        var called = false;
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        var middleware = new ExceptionHandlingMiddleware(
            next: _ => { called = true; return Task.CompletedTask; },
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance,
            environment: new FakeHostEnvironment());

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal(200, context.Response.StatusCode);
    }
}
