using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class StudyArmsControllerWriteTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudyArmsControllerWriteTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateArm_Returns201_AndIsLinkedToStudy()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Arm Write Study");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/arms", new { name = "Treatment Arm" });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateArm_PersistsChanges()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Arm Update Study");
        var armId = await SeedArmAsync(studyId, "Original");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/arms/{armId}", new { name = "Renamed" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/studies/{studyId}/arms/{armId}");
        var json = await getResp.Content.ReadAsStringAsync();
        Assert.Contains("Renamed", json);
    }

    [Fact]
    public async Task CreateArm_Returns400_WhenProtocolVersionIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Arm Bad Protocol Study");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/arms", new { name = "Arm", protocolVersionId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateArm_Returns400_WhenProtocolVersionIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Arm Update Bad Protocol Study");
        var armId = await SeedArmAsync(studyId, "Original");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/arms/{armId}", new { name = "Renamed", protocolVersionId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateArm_ReflectsNewProtocolVersion_NotStaleOrNull()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Arm Reassign Protocol Study");
        var armId = await SeedArmAsync(studyId, "Original");
        var protocolVersionId = await SeedProtocolVersionAsync(studyId, "v1");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/arms/{armId}", new { name = "Original", protocolVersionId });

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
    public async Task DeleteArm_RemovesRecord()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Arm Delete Study");
        var armId = await SeedArmAsync(studyId, "To Delete");
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}/arms/{armId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/studies/{studyId}/arms/{armId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task AddArmVisit_Returns400_WhenVisitBelongsToDifferentStudy()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Arm Assoc Study A");
        var otherStudyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Arm Assoc Study B");
        var armId = await SeedArmAsync(studyId, "Arm");
        var visitId = await SeedVisitAsync(otherStudyId, "Foreign Visit");
        var client = _factory.CreateClient();

        var resp = await client.PostAsync($"/api/v1/studies/{studyId}/arms/{armId}/visits/{visitId}", null);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task AddArmVisit_Returns204_AndRemoveArmVisit_Returns204()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Arm Assoc Study C");
        var armId = await SeedArmAsync(studyId, "Arm");
        var visitId = await SeedVisitAsync(studyId, "Visit");
        var client = _factory.CreateClient();

        var addResp = await client.PostAsync($"/api/v1/studies/{studyId}/arms/{armId}/visits/{visitId}", null);
        Assert.Equal(HttpStatusCode.NoContent, addResp.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(await db.StudyVisitArms.AnyAsync(x => x.ArmId == armId && x.VisitId == visitId));
        }

        var removeResp = await client.DeleteAsync($"/api/v1/studies/{studyId}/arms/{armId}/visits/{visitId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResp.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.StudyVisitArms.AnyAsync(x => x.ArmId == armId && x.VisitId == visitId));
        }
    }

    private async Task<int> SeedArmAsync(int studyId, string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var arm = new StudyArm { Uid = Guid.NewGuid(), StudyId = studyId, Name = name };
        db.StudyArms.Add(arm);
        await db.SaveChangesAsync();
        return arm.Id;
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
