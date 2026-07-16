using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class StudyMilestonesControllerWriteTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudyMilestonesControllerWriteTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateMilestone_Returns201()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Milestone Write Study");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/milestones", new { name = "Site Activation" });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateMilestone_PersistsChanges()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Milestone Update Study");
        var milestoneId = await SeedMilestoneAsync(studyId, "Original");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/milestones/{milestoneId}", new { name = "Renamed", status = "Complete" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Renamed", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateMilestone_Returns400_WhenAssignedToStaffIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Milestone Bad Staff Study");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/milestones", new { name = "Milestone", assignedToStaffId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateMilestone_Returns400_WhenAssignedToStaffIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Milestone Update Bad Staff Study");
        var milestoneId = await SeedMilestoneAsync(studyId, "Original");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/milestones/{milestoneId}", new { name = "Renamed", assignedToStaffId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateMilestone_ReflectsNewAssignedToStaff_NotStaleOrNull()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Milestone Reassign Staff Study");
        var milestoneId = await SeedMilestoneAsync(studyId, "Original");
        var staffId = await SeedStaffAsync("New", "Assignee");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/milestones/{milestoneId}", new { name = "Original", assignedToStaffId = staffId });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(staffId, doc.RootElement.GetProperty("assignedTo").GetProperty("id").GetInt32());
    }

    private async Task<int> SeedStaffAsync(string firstName, string lastName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var staff = new Staff { StaffUid = Guid.NewGuid(), FirstName = firstName, LastName = lastName, IsActive = true };
        db.Staff.Add(staff);
        await db.SaveChangesAsync();
        return staff.Id;
    }

    [Fact]
    public async Task DeleteMilestone_RemovesRecord()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Milestone Delete Study");
        var milestoneId = await SeedMilestoneAsync(studyId, "To Delete");
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}/milestones/{milestoneId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/studies/{studyId}/milestones/{milestoneId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    private async Task<int> SeedMilestoneAsync(int studyId, string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var milestone = new StudyMilestone { StudyId = studyId, Name = name };
        db.StudyMilestones.Add(milestone);
        await db.SaveChangesAsync();
        return milestone.Id;
    }
}
