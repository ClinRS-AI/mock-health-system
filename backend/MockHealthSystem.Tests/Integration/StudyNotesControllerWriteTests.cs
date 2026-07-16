using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class StudyNotesControllerWriteTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudyNotesControllerWriteTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateNote_Returns201()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Note Write Study");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/notes", new { note = "Kickoff scheduled" });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateNote_PersistsChanges()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Note Update Study");
        var noteId = await SeedNoteAsync(studyId, "Original note", locked: false);
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/notes/{noteId}", new { note = "Updated note" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Updated note", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateNote_Returns400_WhenStaffIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Note Bad Staff Study");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/notes", new { note = "Note", staffId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateNote_Returns400_WhenLastUpdatedStaffIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Note Update Bad Staff Study");
        var noteId = await SeedNoteAsync(studyId, "Original note", locked: false);
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/notes/{noteId}", new { note = "Updated", lastUpdatedStaffId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateNote_ReflectsNewStaff_NotStaleOrNull()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Note Reassign Staff Study");
        var noteId = await SeedNoteAsync(studyId, "Original note", locked: false);
        var staffId = await SeedStaffAsync("New", "Author");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/notes/{noteId}", new { note = "Original note", staffId });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(staffId, doc.RootElement.GetProperty("staff").GetProperty("id").GetInt32());
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
    public async Task UpdateNote_Returns409_WhenLocked()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Note Locked Study");
        var noteId = await SeedNoteAsync(studyId, "Locked note", locked: true);
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/notes/{noteId}", new { note = "Attempted update" });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteNote_Returns409_WhenLocked()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Note Locked Delete Study");
        var noteId = await SeedNoteAsync(studyId, "Locked note", locked: true);
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}/notes/{noteId}");

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteNote_RemovesRecord_WhenUnlocked()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Note Delete Study");
        var noteId = await SeedNoteAsync(studyId, "To delete", locked: false);
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}/notes/{noteId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    private async Task<int> SeedNoteAsync(int studyId, string note, bool locked)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = new StudyNote { StudyId = studyId, Note = note, NoteDate = DateTime.UtcNow, Locked = locked };
        db.StudyNotes.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }
}
