using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class MonitoringEndpointTests : IClassFixture<MockHealthSystemWebApplicationFactory>
{
    private readonly MockHealthSystemWebApplicationFactory _factory;

    public MonitoringEndpointTests(MockHealthSystemWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class RequestSummary
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public string? Origin { get; set; }
        public int StatusCode { get; set; }
    }

    [Fact]
    public async Task ExternalRequest_IsLogged_AndVisibleInMonitoringList()
    {
        var client = _factory.CreateClient();

        // Trigger a simple health request (no Origin header) which should be logged.
        var healthResponse = await client.GetAsync("/api/v1/health");
        healthResponse.EnsureSuccessStatusCode();

        // Query monitoring endpoint.
        var monitoringResponse = await client.GetAsync("/api/v1/monitoring/requests");
        monitoringResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, monitoringResponse.StatusCode);

        var json = await monitoringResponse.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<RequestSummary>>(json, JsonOptions) ?? [];

        Assert.NotEmpty(items);
        Assert.Contains(items, x => x.Path.Contains("/api/v1/health", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UiOriginRequest_IsNotLogged()
    {
        var client = _factory.CreateClient();

        // Capture current list size.
        var beforeResponse = await client.GetAsync("/api/v1/monitoring/requests");
        beforeResponse.EnsureSuccessStatusCode();
        var beforeJson = await beforeResponse.Content.ReadAsStringAsync();
        var beforeItems = JsonSerializer.Deserialize<List<RequestSummary>>(beforeJson, JsonOptions) ?? [];

        // Trigger a health request with Origin equal to the frontend URL; this should be excluded by middleware.
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health"))
        {
            request.Headers.Add("Origin", "http://localhost:5174");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        // Query monitoring endpoint again.
        var afterResponse = await client.GetAsync("/api/v1/monitoring/requests");
        afterResponse.EnsureSuccessStatusCode();
        var afterJson = await afterResponse.Content.ReadAsStringAsync();
        var afterItems = JsonSerializer.Deserialize<List<RequestSummary>>(afterJson, JsonOptions) ?? [];

        // Ensure no new entry with this Origin/path combination appeared.
        var newItems = afterItems.Where(a => beforeItems.All(b => b.Id != a.Id)).ToList();
        Assert.DoesNotContain(newItems, x =>
            x.Origin != null &&
            x.Origin.Contains("http://localhost:5174", StringComparison.OrdinalIgnoreCase) &&
            x.Path.Contains("/api/v1/health", StringComparison.OrdinalIgnoreCase));
    }
}

