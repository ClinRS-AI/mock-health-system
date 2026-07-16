using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class StudySubResourceReadTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudySubResourceReadTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Arms_ListAndDetail_ReturnOnlyRecordsForTheStudy()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Arms Study");
        var otherStudyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Other Study");
        int armId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var arm = new StudyArm { Uid = Guid.NewGuid(), StudyId = studyId, Name = "Arm A" };
            db.StudyArms.Add(arm);
            db.StudyArms.Add(new StudyArm { Uid = Guid.NewGuid(), StudyId = otherStudyId, Name = "Other Arm" });
            await db.SaveChangesAsync();
            armId = arm.Id;
        }

        var client = _factory.CreateClient();

        var listResp = await client.GetAsync($"/api/v1/studies/{studyId}/arms");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        using var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        Assert.Single(listDoc.RootElement.EnumerateArray());

        var detailResp = await client.GetAsync($"/api/v1/studies/{studyId}/arms/{armId}");
        Assert.Equal(HttpStatusCode.OK, detailResp.StatusCode);

        var wrongStudyResp = await client.GetAsync($"/api/v1/studies/{otherStudyId}/arms/{armId}");
        Assert.Equal(HttpStatusCode.NotFound, wrongStudyResp.StatusCode);
    }

    [Fact]
    public async Task ArmVisitAssociation_IsReadableFromBothSides()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Assoc Study");
        int armId, visitId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var arm = new StudyArm { Uid = Guid.NewGuid(), StudyId = studyId, Name = "Arm" };
            var visit = new StudyVisit { Uid = Guid.NewGuid(), StudyId = studyId, Name = "Visit", IsActive = true };
            db.StudyArms.Add(arm);
            db.StudyVisits.Add(visit);
            await db.SaveChangesAsync();
            db.StudyVisitArms.Add(new StudyVisitArm { ArmId = arm.Id, VisitId = visit.Id });
            await db.SaveChangesAsync();
            armId = arm.Id;
            visitId = visit.Id;
        }

        var client = _factory.CreateClient();

        var armVisitsResp = await client.GetAsync($"/api/v1/studies/{studyId}/arms/{armId}/visits");
        using var armVisitsDoc = JsonDocument.Parse(await armVisitsResp.Content.ReadAsStringAsync());
        Assert.Single(armVisitsDoc.RootElement.EnumerateArray());

        var visitArmsResp = await client.GetAsync($"/api/v1/studies/{studyId}/visits/{visitId}/arms");
        using var visitArmsDoc = JsonDocument.Parse(await visitArmsResp.Content.ReadAsStringAsync());
        Assert.Single(visitArmsDoc.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task Visits_OdataAndDetail_Work()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Visits Study");
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.StudyVisits.Add(new StudyVisit { Uid = Guid.NewGuid(), StudyId = studyId, Name = "Screening", IsActive = true });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/api/v1/studies/{studyId}/visits/odata");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Milestones_ListOdataAndDetail_Work()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Milestones Study");
        int milestoneId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var m = new StudyMilestone { StudyId = studyId, Name = "Site Activation" };
            db.StudyMilestones.Add(m);
            await db.SaveChangesAsync();
            milestoneId = m.Id;
        }

        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/studies/{studyId}/milestones")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/studies/{studyId}/milestones/odata")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/studies/{studyId}/milestones/{milestoneId}")).StatusCode);
    }

    [Fact]
    public async Task Documents_ListOdataDetailAndHistory_Work()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Documents Study");
        int documentId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var doc = new StudyDocument { Uid = Guid.NewGuid(), StudyId = studyId, StatusName = "Approved" };
            db.StudyDocuments.Add(doc);
            await db.SaveChangesAsync();
            db.StudyDocumentStatusHistories.Add(new StudyDocumentStatusHistory { StudyDocumentId = doc.Id, StatusName = "Approved", ChangedOn = DateTime.UtcNow });
            await db.SaveChangesAsync();
            documentId = doc.Id;
        }

        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/studies/{studyId}/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/studies/{studyId}/documents/odata")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/studies/{studyId}/documents/{documentId}")).StatusCode);

        var historyResp = await client.GetAsync($"/api/v1/studies/{studyId}/documents/{documentId}/history");
        Assert.Equal(HttpStatusCode.OK, historyResp.StatusCode);
        using var historyDoc = JsonDocument.Parse(await historyResp.Content.ReadAsStringAsync());
        Assert.Single(historyDoc.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task Notes_ListOdataAndDetail_Work()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Notes Study");
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.StudyNotes.Add(new StudyNote { StudyId = studyId, Note = "Kickoff call complete", NoteDate = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/studies/{studyId}/notes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/studies/{studyId}/notes/odata")).StatusCode);
    }

    [Fact]
    public async Task Roles_ListAndDetail_IncludeAssignedStaff()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Roles Study");
        int roleId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var staff = new Staff { StaffUid = Guid.NewGuid(), FirstName = "Sam", LastName = "Coordinator", IsActive = true };
            db.Staff.Add(staff);
            var role = new StudyRole { StudyId = studyId, Name = "Coordinator", IsCoordinator = true };
            db.StudyRoles.Add(role);
            await db.SaveChangesAsync();
            db.StudyRoleStaffs.Add(new StudyRoleStaff { StudyRoleId = role.Id, StaffId = staff.Id, Priority = "Primary" });
            await db.SaveChangesAsync();
            roleId = role.Id;
        }

        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/api/v1/studies/{studyId}/roles/{roleId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Single(doc.RootElement.GetProperty("staff").EnumerateArray());
    }

    [Fact]
    public async Task ProtocolVersions_ListAndDetail_Work()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Protocol Study");
        int protocolVersionId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pv = new ProtocolVersion { Uid = Guid.NewGuid(), StudyId = studyId, Name = "v1.0" };
            db.ProtocolVersions.Add(pv);
            await db.SaveChangesAsync();
            protocolVersionId = pv.Id;
        }

        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/studies/{studyId}/protocol-versions")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/studies/{studyId}/protocol-versions/{protocolVersionId}")).StatusCode);
    }

    [Fact]
    public async Task SubResourceEndpoints_Return404_WhenParentStudyMissing()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/studies/900000002/arms")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/studies/900000002/milestones")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/studies/900000002/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/studies/900000002/notes")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/studies/900000002/roles")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/studies/900000002/protocol-versions")).StatusCode);
    }
}
