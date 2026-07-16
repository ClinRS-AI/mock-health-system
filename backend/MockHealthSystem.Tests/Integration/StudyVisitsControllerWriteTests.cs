using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class StudyVisitsControllerWriteTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudyVisitsControllerWriteTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateVisit_Returns201()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Visit Write Study");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/visits", new { name = "Screening Visit" });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateVisit_PersistsChanges()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Visit Update Study");
        var visitId = await SeedVisitAsync(studyId, "Original Visit");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/visits/{visitId}", new { name = "Renamed Visit" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/studies/{studyId}/visits/{visitId}");
        Assert.Contains("Renamed Visit", await getResp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateVisit_Returns400_WhenProtocolVersionIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Visit Bad Protocol Study");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/visits", new { name = "Visit", protocolVersionId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateVisit_Returns400_WhenProtocolVersionIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Visit Update Bad Protocol Study");
        var visitId = await SeedVisitAsync(studyId, "Original Visit");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/visits/{visitId}", new { name = "Renamed", protocolVersionId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateVisit_ReflectsNewProtocolVersion_NotStaleOrNull()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Visit Reassign Protocol Study");
        var visitId = await SeedVisitAsync(studyId, "Original Visit");
        var protocolVersionId = await SeedProtocolVersionAsync(studyId, "v1");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/visits/{visitId}", new { name = "Original Visit", protocolVersionId });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(protocolVersionId, doc.RootElement.GetProperty("protocolVersion").GetProperty("id").GetInt32());
    }

    private async Task<int> SeedProtocolVersionAsync(int studyId, string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pv = new ProtocolVersion { Uid = Guid.NewGuid(), StudyId = studyId, Name = name };
        db.ProtocolVersions.Add(pv);
        await db.SaveChangesAsync();
        return pv.Id;
    }

    [Fact]
    public async Task DeleteVisit_RemovesRecord()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Visit Delete Study");
        var visitId = await SeedVisitAsync(studyId, "To Delete");
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}/visits/{visitId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/studies/{studyId}/visits/{visitId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    private async Task<int> SeedVisitAsync(int studyId, string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var visit = new StudyVisit { Uid = Guid.NewGuid(), StudyId = studyId, Name = name, IsActive = true };
        db.StudyVisits.Add(visit);
        await db.SaveChangesAsync();
        return visit.Id;
    }
}
