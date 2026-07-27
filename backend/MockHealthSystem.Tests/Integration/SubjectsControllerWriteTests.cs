using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Api.Models.Subjects;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class SubjectsControllerWriteTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public SubjectsControllerWriteTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- POST ----

    [Fact]
    public async Task CreateSubject_Returns201_WithGeneratedIdAndUid()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/subjects", new
        {
            patientId,
            studyId,
            status = "Prescreened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("id").GetInt32() > 0);
        Assert.NotEqual(Guid.Empty, doc.RootElement.GetProperty("uid").GetGuid());
    }

    [Fact]
    public async Task CreateSubject_Returns400_WhenPatientIdInvalid()
    {
        var (_, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/subjects", new
        {
            patientId = 999999,
            studyId,
            status = "Prescreened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateSubject_Returns400_WhenStudyIdInvalid()
    {
        var (patientId, _, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/subjects", new
        {
            patientId,
            studyId = 999999,
            status = "Prescreened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateSubject_Returns400_WhenSiteIdInvalid()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/subjects", new
        {
            patientId,
            studyId,
            siteId = 999999,
            status = "Prescreened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateSubject_Returns201_WithNestedSite_WhenValid()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var siteId = await SeedSiteAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/subjects", new
        {
            patientId,
            studyId,
            siteId,
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(siteId, doc.RootElement.GetProperty("site").GetProperty("id").GetInt32());
        Assert.Equal(patientId, doc.RootElement.GetProperty("patient").GetProperty("id").GetInt32());
        Assert.Equal(studyId, doc.RootElement.GetProperty("study").GetProperty("id").GetInt32());
        Assert.Equal(SubjectStatusCatalog.InitialStatus, doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateSubject_IgnoresStatusStudyArmAndProtocolVersion_WhenSentInBody()
    {
        // POST /subjects (SubjectCreateModel) has no status/studyArmId/protocolVersionId
        // properties — CC's real Subject POST body doesn't accept them either. Extra JSON
        // properties are silently dropped by model binding rather than rejected.
        var (patientId, studyId, armId) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var protocolVersionId = await SeedProtocolVersionAsync(studyId);
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/subjects", new
        {
            patientId,
            studyId,
            studyArmId = armId,
            protocolVersionId,
            status = "Active",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(SubjectStatusCatalog.InitialStatus, doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("arm").ValueKind);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("protocolVersion").ValueKind);
    }

    // ---- One-Active-category-per-pair rule ----

    [Fact]
    public async Task CreateSubject_Returns400_WhenAnotherActiveCategorySubjectAlreadyExistsForPair()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Randomized");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/subjects", new
        {
            patientId,
            studyId,
            status = "Screened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateSubject_Succeeds_WhenPriorEnrollmentForPairIsNowInactiveCategory()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Complete");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/subjects", new
        {
            patientId,
            studyId,
            status = "Prescreened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateSubject_Returns400_WhenChangingToActiveCategoryConflictsWithAnotherSubject()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Randomized");
        var toUpdateId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/subjects/{toUpdateId}", new
        {
            patientId,
            studyId,
            status = "Screened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ---- PUT / PATCH ----

    [Fact]
    public async Task UpdateSubject_PersistsChanges()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/subjects/{subjectId}", new
        {
            patientId,
            studyId,
            status = "Screened",
            screeningNumber = "SCR-9001",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/subjects/{subjectId}");
        using var doc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        Assert.Equal("Screened", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("SCR-9001", doc.RootElement.GetProperty("screeningNumber").GetString());
    }

    [Fact]
    public async Task UpdateSubject_Returns400_WhenStatusValueOutsideDefinedVocabulary()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/subjects/{subjectId}", new
        {
            patientId,
            studyId,
            status = "Active",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateSubject_Returns400_WhenStudyArmDoesNotBelongToStudy()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var (_, _, otherArmId) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/subjects/{subjectId}", new
        {
            patientId,
            studyId,
            studyArmId = otherArmId,
            status = "Prescreened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateSubject_Returns400_WhenProtocolVersionDoesNotBelongToStudy()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var (_, otherStudyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var otherProtocolVersionId = await SeedProtocolVersionAsync(otherStudyId);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/subjects/{subjectId}", new
        {
            patientId,
            studyId,
            protocolVersionId = otherProtocolVersionId,
            status = "Prescreened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateSubject_Returns200_WithNestedStudyArmAndProtocolVersion_WhenValid()
    {
        var (patientId, studyId, armId) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var protocolVersionId = await SeedProtocolVersionAsync(studyId);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/subjects/{subjectId}", new
        {
            patientId,
            studyId,
            studyArmId = armId,
            protocolVersionId,
            status = "Prescreened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(armId, doc.RootElement.GetProperty("arm").GetProperty("id").GetInt32());
        Assert.Equal(protocolVersionId, doc.RootElement.GetProperty("protocolVersion").GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task UpdateSubject_Returns404_WhenMissing()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync("/api/v1/subjects/900000001", new
        {
            patientId,
            studyId,
            status = "Screened",
            enrollmentDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task PatchSubject_PersistsOnlyProvidedFields()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var resp = await client.PatchAsJsonAsync($"/api/v1/subjects/{subjectId}", new { status = "Screened" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/subjects/{subjectId}");
        using var doc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        Assert.Equal("Screened", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(patientId, doc.RootElement.GetProperty("patient").GetProperty("id").GetInt32());
    }

    // ---- DELETE ----

    [Fact]
    public async Task DeleteSubject_RemovesRecord_AndItsStatusHistory()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/subjects/{subjectId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/subjects/{subjectId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.SubjectStatuses.AnyAsync(h => h.SubjectId == subjectId));
    }

    // ---- Status-history recording (FR-004) ----

    [Fact]
    public async Task CreateSubject_AppendsInitialStatusHistoryEntry_RetrievableViaStudyStatusEndpoint()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var studyUid = await GetStudyUidAsync(studyId);
        var client = _factory.CreateClient();

        var createResp = await client.PostAsJsonAsync("/api/v1/subjects", new
        {
            patientId,
            studyId,
            status = "Prescreened",
            enrollmentDate = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var historyResp = await client.GetAsync($"/api/v1/studies/{studyUid}/subject-statuses/odata");
        using var doc = JsonDocument.Parse(await historyResp.Content.ReadAsStringAsync());
        var items = doc.RootElement.EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("Prescreened", items[0].GetProperty("statusName").GetString());
    }

    [Fact]
    public async Task UpdateSubject_AppendsStatusHistoryEntry_WhenStatusChanges()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var studyUid = await GetStudyUidAsync(studyId);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var updateResp = await client.PutAsJsonAsync($"/api/v1/subjects/{subjectId}", new
        {
            patientId,
            studyId,
            status = "Screened",
            enrollmentDate = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        var historyResp = await client.GetAsync($"/api/v1/studies/{studyUid}/subject-statuses/odata");
        using var doc = JsonDocument.Parse(await historyResp.Content.ReadAsStringAsync());
        var items = doc.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.GetProperty("statusName").GetString() == "Screened");
    }

    [Fact]
    public async Task UpdateSubject_DoesNotAppendStatusHistoryEntry_WhenStatusUnchanged()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId, "Prescreened");
        var client = _factory.CreateClient();

        var updateResp = await client.PutAsJsonAsync($"/api/v1/subjects/{subjectId}", new
        {
            patientId,
            studyId,
            status = "Prescreened",
            screeningNumber = "SCR-1",
            enrollmentDate = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.SubjectStatuses.CountAsync(h => h.SubjectId == subjectId);
        Assert.Equal(1, count);
    }

    private async Task<Guid> GetStudyUidAsync(int studyId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.Studies.FindAsync(studyId))!.Uid;
    }

    private async Task<int> SeedSiteAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var site = new Site { Uid = Guid.NewGuid(), Name = "Extra Site" };
        db.Sites.Add(site);
        await db.SaveChangesAsync();
        return site.Id;
    }

    private async Task<int> SeedProtocolVersionAsync(int studyId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var protocolVersion = new ProtocolVersion { Uid = Guid.NewGuid(), StudyId = studyId, Name = "v1" };
        db.ProtocolVersions.Add(protocolVersion);
        await db.SaveChangesAsync();
        return protocolVersion.Id;
    }
}
