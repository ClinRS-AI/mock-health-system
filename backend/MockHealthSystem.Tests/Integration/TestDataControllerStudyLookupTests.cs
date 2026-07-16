using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class TestDataControllerStudyLookupTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerStudyLookupTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LookupStudy_ByName_ReturnsMatch()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Lookup Fragment Trial");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/studies/lookup?name=Lookup+Fragment");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(studyId, doc.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task LookupStudy_ByName_IsCaseInsensitive()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Case Sensitive Trial");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/studies/lookup?name=case+sensitive");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(studyId, doc.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task LookupStudy_ByName_TrimsWhitespace()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Whitespace Trial");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/studies/lookup?name=%20%20Whitespace%20");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(studyId, doc.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task LookupStudy_ByProtocolNumberFragment_ReturnsMatch()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Protocol Lookup Study", s => s.ProtocolNumber = "PROTO-2026-777");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/studies/lookup?protocolNumber=PROTO-2026");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(studyId, doc.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task LookupStudy_Returns400_WhenNoCriteriaProvided()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/test-data/studies/lookup");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task LookupStudy_Returns404_WhenNoMatch()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/test-data/studies/lookup?name=DoesNotExist");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetRandomStudy_Returns200_WhenStudiesExist()
    {
        await StudySeedHelpers.SeedStudyAsync(_factory, "Random Pool Study");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/studies/random");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetStudyStats_ReturnsCounts()
    {
        await StudySeedHelpers.SeedFullStudyAsync(_factory, "Stats Study");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/studies/stats");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("studyCount").GetInt32() >= 1);
    }
}
