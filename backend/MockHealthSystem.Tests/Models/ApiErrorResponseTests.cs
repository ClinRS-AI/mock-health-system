using MockHealthSystem.Api.Models;
using Xunit;

namespace MockHealthSystem.Tests.Models;

public sealed class ApiErrorResponseTests
{
    [Fact]
    public void ApiErrorResponse_Properties_ReflectConstructorValues()
    {
        var response = new ApiErrorResponse
        {
            Status = 400,
            Title = "Bad Request",
            Detail = "Invalid input",
            TraceId = "trace-123"
        };

        Assert.Equal(400, response.Status);
        Assert.Equal("Bad Request", response.Title);
        Assert.Equal("Invalid input", response.Detail);
        Assert.Equal("trace-123", response.TraceId);
    }

    [Fact]
    public void ApiErrorResponse_OptionalDetail_CanBeNull()
    {
        var response = new ApiErrorResponse
        {
            Status = 500,
            Title = "Error",
            Detail = null,
            TraceId = null
        };

        Assert.Null(response.Detail);
        Assert.Null(response.TraceId);
    }
}
