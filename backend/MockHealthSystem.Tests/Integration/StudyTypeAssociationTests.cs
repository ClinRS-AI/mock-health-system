using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class StudyTypeAssociationTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudyTypeAssociationTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddStudyType_Returns204_AndPersistsAssociation()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Type Assoc Study");
        var studyTypeId = await SeedStudyTypeAsync("Interventional");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/types/add", new { studyTypeId });

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.StudyStudyTypes.AnyAsync(x => x.StudyId == studyId && x.StudyTypeId == studyTypeId));
    }

    [Fact]
    public async Task AddStudyType_Returns400_WhenStudyTypeIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Type Assoc Study 2");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/types/add", new { studyTypeId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task RemoveStudyType_Returns204_AndRemovesAssociation()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Type Remove Study");
        var studyTypeId = await SeedStudyTypeAsync("Observational");
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/types/add", new { studyTypeId });

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}/types/{studyTypeId}");

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.StudyStudyTypes.AnyAsync(x => x.StudyId == studyId && x.StudyTypeId == studyTypeId));
    }

    [Fact]
    public async Task RemoveStudyType_Returns404_WhenAssociationMissing()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Type Remove Missing");
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}/types/999999");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    private async Task<int> SeedStudyTypeAsync(string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var type = new StudyType { Name = name };
        db.StudyTypes.Add(type);
        await db.SaveChangesAsync();
        return type.Id;
    }
}
