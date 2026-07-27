using System.Net;
using System.Text.Json;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class TestDataControllerSubjectLookupTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectLookupTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LookupSubject_ById_ReturnsMatch()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/test-data/subjects/lookup?id={subjectId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(subjectId, doc.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task LookupSubject_ByPatientIdAndStudyId_ReturnsMatch()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/test-data/subjects/lookup?patientId={patientId}&studyId={studyId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(subjectId, doc.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task LookupSubject_Returns400_WhenNoCriteriaProvided()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/test-data/subjects/lookup");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task LookupSubject_Returns404_WhenNoMatch()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/test-data/subjects/lookup?id=900000001");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetRandomSubject_Returns200_WhenSubjectsExist()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/subjects/random");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}

/// <summary>Isolated in its own class (own factory/DB instance) because "no subjects exist yet"
/// is a genuinely empty precondition that other tests sharing a class-level fixture would
/// otherwise pollute — see IsolatedWebApplicationFactory's "one DB per test class" doc comment.</summary>
public sealed class TestDataControllerSubjectRandomEmptyTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectRandomEmptyTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRandomSubject_Returns404_WhenNoSubjectsExist()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/test-data/subjects/random");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}

/// <summary>Isolated in its own class because the exact-count assertions here would be polluted
/// by subjects seeded in other test classes' shared fixtures.</summary>
public sealed class TestDataControllerSubjectStatsTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectStatsTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSubjectStats_ReturnsTotalCount_AndDistinctPatientsPerStudy()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        // Two enrollment episodes for the same patient/study pair (different, non-conflicting statuses).
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Complete");
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/subjects/stats");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("subjectCount").GetInt32());

        var byStudy = doc.RootElement.GetProperty("patientsByStudy").EnumerateArray().ToList();
        var entry = Assert.Single(byStudy, e => e.GetProperty("studyId").GetInt32() == studyId);
        // Same patient enrolled twice in the same study counts once (distinct patients).
        Assert.Equal(1, entry.GetProperty("patientCount").GetInt32());

        var topStudies = doc.RootElement.GetProperty("topStudiesBySubjectStatus").EnumerateArray().ToList();
        var studyBreakdown = Assert.Single(topStudies, e => e.GetProperty("studyId").GetInt32() == studyId);
        Assert.Equal(2, studyBreakdown.GetProperty("totalCount").GetInt32());
        var byStatus = studyBreakdown.GetProperty("byStatus").EnumerateArray().ToList();
        Assert.Equal(2, byStatus.Count);
        Assert.Contains(byStatus, s => s.GetProperty("statusName").GetString() == "Complete" && s.GetProperty("count").GetInt32() == 1);
        Assert.Contains(byStatus, s => s.GetProperty("statusName").GetString() == "Prescreened" && s.GetProperty("count").GetInt32() == 1);
    }
}

/// <summary>Isolated in its own class because seeding 12 distinct studies here would otherwise
/// pollute the exact `subjectCount`/`patientsByStudy` assertions in TestDataControllerSubjectStatsTests.</summary>
public sealed class TestDataControllerSubjectStatsCapTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectStatsCapTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSubjectStats_TopStudiesBySubjectStatus_CapsAtTenStudies()
    {
        for (var i = 0; i < 12; i++)
        {
            var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
            await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        }
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/subjects/stats");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var topStudies = doc.RootElement.GetProperty("topStudiesBySubjectStatus").EnumerateArray().ToList();
        Assert.Equal(10, topStudies.Count);
    }
}
