using System.Net;
using System.Text.Json;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class SubjectsControllerTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SubjectsControllerTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSubject_Returns404_WhenMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/subjects/900000001");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetSubject_Returns200_WithCoreFields()
    {
        var (patientId, studyId, armId) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Screened", armId);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/subjects/{subjectId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(subjectId, root.GetProperty("id").GetInt32());
        Assert.Equal(patientId, root.GetProperty("patient").GetProperty("id").GetInt32());
        Assert.Equal(studyId, root.GetProperty("study").GetProperty("id").GetInt32());
        Assert.Equal(armId, root.GetProperty("arm").GetProperty("id").GetInt32());
        Assert.Equal("Screened", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("enrollmentDate", out _));
    }

    [Fact]
    public async Task GetSubjects_FiltersByPatientId()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var (otherPatientId, otherStudyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, otherPatientId, otherStudyId);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/subjects?patientId={patientId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.Equal(patientId, i.GetProperty("patient").GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task GetSubjects_FiltersByStudyId()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var (otherPatientId, otherStudyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, otherPatientId, otherStudyId);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/subjects?studyId={studyId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.Equal(studyId, i.GetProperty("study").GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task GetSubjects_RespectsSkipAndLimit()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        for (var i = 0; i < 5; i++)
        {
            await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Complete");
        }
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/subjects?studyId={studyId}&skip=1&limit=2");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetSubjectsOData_ReturnsPagedWrapper_WithItemsAndCount()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/subjects/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await DeserializeOdataResult<SubjectItem>(resp);
        Assert.NotNull(result);
        Assert.True(result!.Items.Count >= 1);
        Assert.True(result.Count >= 1);
    }

    [Fact]
    public async Task GetSubjectsOData_FiltersByStudyId()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var (otherPatientId, otherStudyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, otherPatientId, otherStudyId);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/subjects/odata?studyId={studyId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await DeserializeOdataResult<SubjectItem>(resp);
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Items);
        Assert.All(result.Items, i => Assert.Equal(studyId, i.Study.Id));
    }

    [Fact]
    public async Task GetSubjectsOData_RespectsSkipAndTopPaging()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        for (var i = 0; i < 4; i++)
        {
            await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);
        }
        var client = _factory.CreateClient();

        var allResp = await client.GetAsync($"/api/v1/subjects/odata?studyId={studyId}&top=100");
        var allResult = await DeserializeOdataResult<SubjectItem>(allResp);

        var skipResp = await client.GetAsync($"/api/v1/subjects/odata?studyId={studyId}&skip=1&top=100");
        var skipResult = await DeserializeOdataResult<SubjectItem>(skipResp);

        Assert.Equal(allResult!.Items.Count - 1, skipResult!.Items.Count);
        Assert.Equal(allResult.Items[1].Id, skipResult.Items[0].Id);
        Assert.Equal(allResult.Count, skipResult.Count);
    }

    private static async Task<ODataResult<T>?> DeserializeOdataResult<T>(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ODataResult<T>>(json, JsonOptions);
    }

    private sealed class ODataResult<T>
    {
        public List<T> Items { get; set; } = [];
        public long Count { get; set; }
        public string? NextPageLink { get; set; }
    }

    private sealed class SubjectItem
    {
        public int Id { get; set; }
        public StudyItem Study { get; set; } = null!;
    }

    private sealed class StudyItem
    {
        public int Id { get; set; }
    }
}
