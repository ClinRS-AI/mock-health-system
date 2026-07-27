using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class SubjectStatusesControllerTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public SubjectStatusesControllerTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSubjectStatusesOData_Returns404_WhenStudyUidUnknown()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/api/v1/studies/{Guid.NewGuid()}/subject-statuses/odata");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetSubjectStatusesOData_ReturnsEmptyArray_WhenStudyHasNoSubjects()
    {
        var (_, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var studyUid = await GetStudyUidAsync(studyId);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/studies/{studyUid}/subject-statuses/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetSubjectStatusesOData_ReturnsEntriesOrderedByChangedOnDescending()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var studyUid = await GetStudyUidAsync(studyId);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Screened");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/studies/{studyUid}/subject-statuses/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, items.Count);

        var changedOnValues = items.Select(i => i.GetProperty("changedOn").GetDateTime()).ToList();
        Assert.True(changedOnValues[0] >= changedOnValues[1]);
    }

    private async Task<Guid> GetStudyUidAsync(int studyId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.Studies.FindAsync(studyId))!.Uid;
    }
}
