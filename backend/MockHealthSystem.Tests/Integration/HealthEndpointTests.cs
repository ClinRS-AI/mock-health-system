using System.Net;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class HealthEndpointTests : IClassFixture<MockHealthSystemWebApplicationFactory>
{
    private readonly MockHealthSystemWebApplicationFactory _factory;

    public HealthEndpointTests(MockHealthSystemWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsOk_WithStatusMessage()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Mock Health System API is running", content);
    }
}
