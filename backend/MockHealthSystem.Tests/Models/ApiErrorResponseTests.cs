using MockHealthSystem.Api.Models;
using System.Text.Json;
using Xunit;

namespace MockHealthSystem.Tests.Models;

public sealed class ApiErrorResponseTests
{
    [Fact]
    public void ApiErrorResponse_SerializesExpectedFields()
    {
        var response = new ApiErrorResponse
        {
            Status = 400,
            Title = "Bad Request",
            Detail = "Invalid input",
            TraceId = "trace-123"
        };

        var json = JsonSerializer.Serialize(response);

        Assert.Contains("\"Status\":400", json, StringComparison.Ordinal);
        Assert.Contains("\"Title\":\"Bad Request\"", json, StringComparison.Ordinal);
        Assert.Contains("\"Detail\":\"Invalid input\"", json, StringComparison.Ordinal);
        Assert.Contains("\"TraceId\":\"trace-123\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiErrorResponse_RoundTripsWithNullOptionalFields()
    {
        var response = new ApiErrorResponse
        {
            Status = 500,
            Title = "Error",
            Detail = null,
            TraceId = null
        };

        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<ApiErrorResponse>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(500, deserialized!.Status);
        Assert.Equal("Error", deserialized.Title);
        Assert.Null(deserialized.Detail);
        Assert.Null(deserialized.TraceId);
    }
}
