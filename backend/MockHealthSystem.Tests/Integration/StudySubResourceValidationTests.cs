using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>Sub-resource writes referencing a non-existent or wrong-parent studyId must return 404/400 (FR-005).</summary>
public sealed class StudySubResourceValidationTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private const int MissingStudyId = 900000004;

    private readonly IsolatedWebApplicationFactory _factory;

    public StudySubResourceValidationTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateArm_Returns404_WhenStudyMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{MissingStudyId}/arms", new { name = "Arm" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CreateVisit_Returns404_WhenStudyMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{MissingStudyId}/visits", new { name = "Visit" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CreateMilestone_Returns404_WhenStudyMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{MissingStudyId}/milestones", new { name = "Milestone" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CreateDocument_Returns404_WhenStudyMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{MissingStudyId}/documents", new { statusName = "Draft" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CreateNote_Returns404_WhenStudyMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{MissingStudyId}/notes", new { note = "Note" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProtocolVersion_Returns404_WhenStudyMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{MissingStudyId}/protocol-versions", new { name = "v1" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetArm_Returns404_WhenArmBelongsToDifferentStudy()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Wrong Parent A");
        var otherStudyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Wrong Parent B");
        var client = _factory.CreateClient();

        var createResp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/arms", new { name = "Arm" });
        var created = await createResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var armId = created.GetProperty("id").GetInt32();

        var resp = await client.GetAsync($"/api/v1/studies/{otherStudyId}/arms/{armId}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateArm_Returns404_WhenArmBelongsToDifferentStudy()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Wrong Parent Update A");
        var otherStudyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Wrong Parent Update B");
        var client = _factory.CreateClient();

        var createResp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/arms", new { name = "Arm" });
        var created = await createResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var armId = created.GetProperty("id").GetInt32();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{otherStudyId}/arms/{armId}", new { name = "Hijacked" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
