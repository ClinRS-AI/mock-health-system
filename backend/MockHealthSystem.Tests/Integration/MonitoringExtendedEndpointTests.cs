using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Extended integration tests for MonitoringController:
/// - GetRequestsAsync: filter by pathPrefix / statusCode / sinceUtc, take clamping
/// - GetRequestAsync: detail model, 404
/// - GetStatsAsync: status breakdown, duration aggregations, empty-data case
/// </summary>
public sealed class MonitoringExtendedEndpointTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MonitoringExtendedEndpointTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // =====================================================================
    // GET /monitoring/requests — filter and paging behaviour
    // =====================================================================

    [Fact]
    public async Task GetRequests_ReturnsAllLogs_WhenNoFiltersApplied()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(
            MakeLog("/api/v1/health", 200, 5, IntegrationTestClock.Step(1)),
            MakeLog("/api/v1/patients/1", 404, 10, IntegrationTestClock.Step(2)));

        var resp = await _factory.CreateClient().GetAsync("/api/v1/monitoring/requests");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await DeserializeListAsync<RequestSummary>(resp);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetRequests_FiltersByPathPrefix()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(
            MakeLog("/api/v1/health", 200, 5, IntegrationTestClock.Step(1)),
            MakeLog("/api/v1/patients/42", 200, 8, IntegrationTestClock.Step(2)),
            MakeLog("/api/v1/patients/99", 404, 3, IntegrationTestClock.Step(3)));

        var resp = await _factory.CreateClient()
            .GetAsync("/api/v1/monitoring/requests?pathPrefix=/api/v1/patients");

        var items = await DeserializeListAsync<RequestSummary>(resp);
        Assert.Equal(2, items.Count);
        Assert.All(items, x => Assert.StartsWith("/api/v1/patients", x.Path));
    }

    [Fact]
    public async Task GetRequests_FiltersByStatusCode()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(
            MakeLog("/api/v1/a", 200, 5, IntegrationTestClock.Step(1)),
            MakeLog("/api/v1/b", 200, 7, IntegrationTestClock.Step(2)),
            MakeLog("/api/v1/c", 404, 3, IntegrationTestClock.Step(3)));

        var resp = await _factory.CreateClient()
            .GetAsync("/api/v1/monitoring/requests?statusCode=200");

        var items = await DeserializeListAsync<RequestSummary>(resp);
        Assert.Equal(2, items.Count);
        Assert.All(items, x => Assert.Equal(200, x.StatusCode));
    }

    [Fact]
    public async Task GetRequests_FiltersBySinceUtc()
    {
        await ClearLogsAsync();
        var old = MakeLog("/api/v1/old", 200, 5);
        old.CreatedAtUtc = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var recent = MakeLog("/api/v1/recent", 200, 8);
        recent.CreatedAtUtc = new DateTime(2024, 1, 1, 11, 0, 0, DateTimeKind.Utc);
        await SeedLogsAsync(old, recent);

        var cutoff = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc).ToString("o");
        var resp = await _factory.CreateClient()
            .GetAsync($"/api/v1/monitoring/requests?sinceUtc={Uri.EscapeDataString(cutoff)}");

        var items = await DeserializeListAsync<RequestSummary>(resp);
        Assert.Single(items);
        Assert.Equal("/api/v1/recent", items[0].Path);
    }

    [Fact]
    public async Task GetRequests_RespectsExplicitTakeParameter()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(Enumerable.Range(1, 5).Select(i =>
            MakeLog($"/api/v1/r{i}", 200, i, IntegrationTestClock.Step(i))).ToArray());

        var resp = await _factory.CreateClient()
            .GetAsync("/api/v1/monitoring/requests?take=3");

        var items = await DeserializeListAsync<RequestSummary>(resp);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task GetRequests_DefaultsTo100_WhenTakeIsZero()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(Enumerable.Range(1, 150).Select(i =>
            MakeLog($"/api/v1/many{i}", 200, 1, IntegrationTestClock.Step(i))).ToArray());

        var resp = await _factory.CreateClient()
            .GetAsync("/api/v1/monitoring/requests?take=0");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await DeserializeListAsync<RequestSummary>(resp);
        Assert.Equal(100, items.Count);
    }

    [Fact]
    public async Task GetRequests_DefaultsTo100_WhenTakeIsNegative()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(Enumerable.Range(1, 150).Select(i =>
            MakeLog($"/api/v1/neg{i}", 200, 1, IntegrationTestClock.Step(i))).ToArray());

        var resp = await _factory.CreateClient()
            .GetAsync("/api/v1/monitoring/requests?take=-10");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await DeserializeListAsync<RequestSummary>(resp);
        Assert.Equal(100, items.Count);
    }

    [Fact]
    public async Task GetRequests_CapsTakeTo500()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(Enumerable.Range(1, 520).Select(i =>
            MakeLog($"/api/v1/cap{i}", 200, 1, IntegrationTestClock.Step(i))).ToArray());

        var resp = await _factory.CreateClient()
            .GetAsync("/api/v1/monitoring/requests?take=9999");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await DeserializeListAsync<RequestSummary>(resp);
        Assert.Equal(500, items.Count);
    }

    // =====================================================================
    // GET /monitoring/requests/{id} — detail endpoint
    // =====================================================================

    [Fact]
    public async Task GetRequest_Returns200_WithAllFieldsPopulated()
    {
        await ClearLogsAsync();
        var log = new ApiRequestLog
        {
            CreatedAtUtc = IntegrationTestClock.UtcEpoch,
            Method = "POST",
            Path = "/api/v1/patients",
            QueryString = "?debug=1",
            StatusCode = 201,
            DurationMs = 42,
            Origin = "https://app.example.com",
            Referer = "https://app.example.com/patients",
            UserAgent = "TestAgent/1.0",
            RemoteIp = "127.0.0.1",
            RequestBody = "{\"firstName\":\"Test\"}",
            ResponseBody = "{\"id\":7}",
            CorrelationId = "trace-abc-123"
        };
        var logId = await SeedSingleLogAsync(log);

        var resp = await _factory.CreateClient()
            .GetAsync($"/api/v1/monitoring/requests/{logId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var detail = await DeserializeSingleAsync<RequestDetail>(resp);
        Assert.Equal("POST", detail!.Method);
        Assert.Equal("/api/v1/patients", detail.Path);
        Assert.Equal("?debug=1", detail.QueryString);
        Assert.Equal(201, detail.StatusCode);
        Assert.Equal(42, detail.DurationMs);
        Assert.Equal("https://app.example.com", detail.Origin);
        Assert.Equal("https://app.example.com/patients", detail.Referer);
        Assert.Equal("TestAgent/1.0", detail.UserAgent);
        Assert.Equal("127.0.0.1", detail.RemoteIp);
        Assert.Equal("{\"firstName\":\"Test\"}", detail.RequestBody);
        Assert.Equal("{\"id\":7}", detail.ResponseBody);
        Assert.Equal("trace-abc-123", detail.CorrelationId);
    }

    [Fact]
    public async Task GetRequest_Returns404_WhenLogDoesNotExist()
    {
        var resp = await _factory.CreateClient()
            .GetAsync("/api/v1/monitoring/requests/999999");

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // =====================================================================
    // GET /monitoring/stats — aggregation
    // =====================================================================

    [Fact]
    public async Task GetStats_ReturnsZeroRequestCount_WhenNoLogsExist()
    {
        await ClearLogsAsync();

        var resp = await _factory.CreateClient().GetAsync("/api/v1/monitoring/stats");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var stats = await DeserializeSingleAsync<StatsResult>(resp);
        Assert.Equal(0, stats!.RequestCount);
    }

    [Fact]
    public async Task GetStats_ReturnsNullDurationFields_WhenNoLogsExist()
    {
        await ClearLogsAsync();

        var resp = await _factory.CreateClient().GetAsync("/api/v1/monitoring/stats");

        var stats = await DeserializeSingleAsync<StatsResult>(resp);
        Assert.Null(stats!.AverageDurationMs);
        Assert.Null(stats.Percentile95DurationMs);
        Assert.Null(stats.MaxDurationMs);
    }

    [Fact]
    public async Task GetStats_ReturnsStatusBreakdown_ByStatusCode()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(
            MakeLog("/a", 200, 10, IntegrationTestClock.Step(1)),
            MakeLog("/b", 200, 20, IntegrationTestClock.Step(2)),
            MakeLog("/c", 404, 5, IntegrationTestClock.Step(3)),
            MakeLog("/d", 500, 100, IntegrationTestClock.Step(4)));

        var resp = await _factory.CreateClient().GetAsync("/api/v1/monitoring/stats");

        var stats = await DeserializeSingleAsync<StatsResult>(resp);
        Assert.Equal(4, stats!.RequestCount);
        var breakdown = stats.StatusBreakdown;
        Assert.Equal(2, breakdown.First(x => x.StatusCode == 200).Count);
        Assert.Equal(1, breakdown.First(x => x.StatusCode == 404).Count);
        Assert.Equal(1, breakdown.First(x => x.StatusCode == 500).Count);
    }

    [Fact]
    public async Task GetStats_CalculatesCorrectAverageDuration()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(
            MakeLog("/a", 200, 10, IntegrationTestClock.Step(1)),
            MakeLog("/b", 200, 30, IntegrationTestClock.Step(2)));

        var resp = await _factory.CreateClient().GetAsync("/api/v1/monitoring/stats");

        var stats = await DeserializeSingleAsync<StatsResult>(resp);
        Assert.Equal(20.0, stats!.AverageDurationMs);
    }

    [Fact]
    public async Task GetStats_CalculatesCorrectMaxDuration()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(
            MakeLog("/a", 200, 15, IntegrationTestClock.Step(1)),
            MakeLog("/b", 200, 7, IntegrationTestClock.Step(2)),
            MakeLog("/c", 200, 99, IntegrationTestClock.Step(3)));

        var resp = await _factory.CreateClient().GetAsync("/api/v1/monitoring/stats");

        var stats = await DeserializeSingleAsync<StatsResult>(resp);
        Assert.Equal(99, stats!.MaxDurationMs);
    }

    [Fact]
    public async Task GetStats_CalculatesPercentile95_FromSortedDurations()
    {
        await ClearLogsAsync();
        // 10 values: 1..10 ms. P95 index = ceil(0.95 * 10) - 1 = ceil(9.5) - 1 = 10 - 1 = 9 → sorted[9] = 10.
        await SeedLogsAsync(Enumerable.Range(1, 10).Select(i =>
            MakeLog($"/p{i}", 200, i, IntegrationTestClock.Step(i))).ToArray());

        var resp = await _factory.CreateClient().GetAsync("/api/v1/monitoring/stats");

        var stats = await DeserializeSingleAsync<StatsResult>(resp);
        Assert.Equal(10.0, stats!.Percentile95DurationMs);
    }

    [Fact]
    public async Task GetStats_StatusBreakdownIsOrderedByStatusCode()
    {
        await ClearLogsAsync();
        await SeedLogsAsync(
            MakeLog("/a", 500, 1, IntegrationTestClock.Step(1)),
            MakeLog("/b", 200, 1, IntegrationTestClock.Step(2)),
            MakeLog("/c", 404, 1, IntegrationTestClock.Step(3)));

        var resp = await _factory.CreateClient().GetAsync("/api/v1/monitoring/stats");

        var stats = await DeserializeSingleAsync<StatsResult>(resp);
        var codes = stats!.StatusBreakdown.Select(x => x.StatusCode).ToList();
        Assert.Equal(codes.OrderBy(x => x).ToList(), codes);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static ApiRequestLog MakeLog(string path, int statusCode, int durationMs, DateTime? createdAtUtc = null) =>
        new()
        {
            CreatedAtUtc = createdAtUtc ?? IntegrationTestClock.UtcEpoch,
            Method = "GET",
            Path = path,
            StatusCode = statusCode,
            DurationMs = durationMs
        };

    private async Task SeedLogsAsync(params ApiRequestLog[] logs)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ApiRequestLogs.AddRange(logs);
        await db.SaveChangesAsync();
    }

    private async Task<int> SeedSingleLogAsync(ApiRequestLog log)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ApiRequestLogs.Add(log);
        await db.SaveChangesAsync();
        return log.Id;
    }

    private async Task ClearLogsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ApiRequestLogs.RemoveRange(db.ApiRequestLogs);
        await db.SaveChangesAsync();
    }

    private static async Task<List<T>> DeserializeListAsync<T>(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
    }

    private static async Task<T?> DeserializeSingleAsync<T>(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private sealed class RequestSummary
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public int DurationMs { get; set; }
    }

    private sealed class RequestDetail
    {
        public int Id { get; set; }
        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? QueryString { get; set; }
        public int StatusCode { get; set; }
        public int DurationMs { get; set; }
        public string? Origin { get; set; }
        public string? Referer { get; set; }
        public string? UserAgent { get; set; }
        public string? RemoteIp { get; set; }
        public string? RequestBody { get; set; }
        public string? ResponseBody { get; set; }
        public string? CorrelationId { get; set; }
    }

    private sealed class StatsResult
    {
        public int RequestCount { get; set; }
        public double? AverageDurationMs { get; set; }
        public double? Percentile95DurationMs { get; set; }
        public int? MaxDurationMs { get; set; }
        public List<StatusEntry> StatusBreakdown { get; set; } = [];
    }

    private sealed class StatusEntry
    {
        public int StatusCode { get; set; }
        public int Count { get; set; }
    }
}
