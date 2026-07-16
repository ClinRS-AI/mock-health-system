using System.Net;
using System.Text.Json;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class StudiesControllerTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudiesControllerTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStudy_Returns404_WhenMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/studies/900000001");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetStudy_Returns200_WithFullShape_IncludingContacts()
    {
        var studyId = await StudySeedHelpers.SeedFullStudyAsync(_factory);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/studies/{studyId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(studyId, root.GetProperty("id").GetInt32());
        Assert.Equal("Full Study", root.GetProperty("name").GetString());
        Assert.True(root.GetProperty("sponsorTeam").GetProperty("id").GetInt32() > 0);
        Assert.Equal("Fixed", root.GetProperty("finances").GetProperty("financeType").GetString());
        Assert.Equal("High", root.GetProperty("opportunityDetails").GetProperty("opportunityLevel").GetString());

        var contacts = root.GetProperty("contacts");
        Assert.Equal(1, contacts.GetArrayLength());
        Assert.Equal("Irb", contacts[0].GetProperty("type").GetString());
        Assert.Equal(1, contacts[0].GetProperty("slot").GetInt32());

        var targetDates = root.GetProperty("targetDates");
        Assert.Equal(1, targetDates.GetArrayLength());

        var leadership = root.GetProperty("leadership");
        Assert.Equal(1, leadership.GetArrayLength());
        Assert.NotNull(leadership[0].GetProperty("staff").GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task GetStudies_ReturnsPaginatedList_FilteredByName()
    {
        await StudySeedHelpers.SeedStudyAsync(_factory, "Alpha Trial");
        await StudySeedHelpers.SeedStudyAsync(_factory, "Beta Trial");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/studies?name=Alpha");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.StartsWith("Alpha", i.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task GetStudiesOData_ReturnsSimpleList()
    {
        await StudySeedHelpers.SeedStudyAsync(_factory, "OData Trial");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/studies/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetStudyPersonnel_AggregatesLeadershipAndRoleStaff()
    {
        var studyId = await StudySeedHelpers.SeedFullStudyAsync(_factory);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/studies/{studyId}/personnel");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetArrayLength() >= 1);
    }
}
