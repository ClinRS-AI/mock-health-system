using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class ProtocolVersionsControllerWriteTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public ProtocolVersionsControllerWriteTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProtocolVersion_Returns201()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "PV Write Study");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/protocol-versions", new { name = "v1.0" });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProtocolVersion_PersistsChanges()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "PV Update Study");
        var pvId = await SeedProtocolVersionAsync(studyId, "v1.0");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/protocol-versions/{pvId}", new { name = "v2.0" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("v2.0", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeleteProtocolVersion_Returns409_WhenReferencedByArm()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "PV Referenced Study");
        var pvId = await SeedProtocolVersionAsync(studyId, "v1.0");
        await SeedArmReferencingProtocolVersionAsync(studyId, pvId);
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}/protocol-versions/{pvId}");

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteProtocolVersion_RemovesRecord_WhenUnreferenced()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "PV Delete Study");
        var pvId = await SeedProtocolVersionAsync(studyId, "v1.0");
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}/protocol-versions/{pvId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
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

    private async Task SeedArmReferencingProtocolVersionAsync(int studyId, int protocolVersionId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.StudyArms.Add(new StudyArm { Uid = Guid.NewGuid(), StudyId = studyId, Name = "Arm", ProtocolVersionId = protocolVersionId });
        await db.SaveChangesAsync();
    }
}
