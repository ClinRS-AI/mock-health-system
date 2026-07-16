using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class StudiesControllerWriteTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudiesControllerWriteTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateStudy_Returns201_WithGeneratedIdAndUid()
    {
        var (sponsorTeamId, _, _) = await SeedPrerequisitesAsync();
        var client = _factory.CreateClient();

        var payload = new
        {
            sponsorTeamId,
            name = "New Study",
            status = "Enrolling"
        };

        var resp = await client.PostAsJsonAsync("/api/v1/studies", payload);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("id").GetInt32() > 0);
        Assert.NotEqual(Guid.Empty, doc.RootElement.GetProperty("uid").GetGuid());
    }

    [Fact]
    public async Task CreateStudy_Returns400_WhenSponsorTeamIdInvalid()
    {
        var client = _factory.CreateClient();
        var payload = new { sponsorTeamId = 999999, name = "Bad Study", status = "Enrolling" };

        var resp = await client.PostAsJsonAsync("/api/v1/studies", payload);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateStudy_Returns400_WhenManagingSiteIdInvalid()
    {
        var (sponsorTeamId, _, _) = await SeedPrerequisitesAsync();
        var client = _factory.CreateClient();

        var payload = new { sponsorTeamId, managingSiteId = 999999, name = "Bad Site Study", status = "Enrolling" };
        var resp = await client.PostAsJsonAsync("/api/v1/studies", payload);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateStudy_Returns400_WhenStudyLeadStaffIdInvalid()
    {
        var (sponsorTeamId, _, _) = await SeedPrerequisitesAsync();
        var client = _factory.CreateClient();

        var payload = new
        {
            sponsorTeamId,
            name = "Bad Lead Study",
            status = "Enrolling",
            studyLead = new { staffId = 999999 }
        };
        var resp = await client.PostAsJsonAsync("/api/v1/studies", payload);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateStudy_Returns400_WhenLeadershipStaffIdInvalid()
    {
        var (sponsorTeamId, _, _) = await SeedPrerequisitesAsync();
        var client = _factory.CreateClient();

        var payload = new
        {
            sponsorTeamId,
            name = "Bad Leadership Study",
            status = "Enrolling",
            leadership = new[] { new { name = "PI", required = true, staffId = 999999 } }
        };
        var resp = await client.PostAsJsonAsync("/api/v1/studies", payload);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateStudy_Returns400_WhenContactsHaveDuplicateTypeAndSlot()
    {
        var (sponsorTeamId, _, _) = await SeedPrerequisitesAsync();
        var client = _factory.CreateClient();

        var payload = new
        {
            sponsorTeamId,
            name = "Duplicate Contact Study",
            status = "Enrolling",
            contacts = new[]
            {
                new { type = "Irb", slot = 1, name = "First IRB" },
                new { type = "Irb", slot = 1, name = "Second IRB" }
            }
        };
        var resp = await client.PostAsJsonAsync("/api/v1/studies", payload);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateStudy_ReflectsNewManagingSite_NotStaleOrNull()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Reassign Site Study");
        var sponsorTeamId = await GetSponsorTeamIdAsync(studyId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var newSite = new Site { Uid = Guid.NewGuid(), Name = "New Site" };
        db.Sites.Add(newSite);
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();
        var payload = new { sponsorTeamId, managingSiteId = newSite.Id, name = "Reassign Site Study", status = "Enrolling" };
        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}", payload);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(newSite.Id, doc.RootElement.GetProperty("managingSite").GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task UpdateStudy_ReflectsNewLeadershipStaff_NotStaleOrNull()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Reassign Leadership Study");
        var sponsorTeamId = await GetSponsorTeamIdAsync(studyId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var newStaff = new Staff { StaffUid = Guid.NewGuid(), FirstName = "New", LastName = "Leader", IsActive = true };
        db.Staff.Add(newStaff);
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();
        var payload = new
        {
            sponsorTeamId,
            name = "Reassign Leadership Study",
            status = "Enrolling",
            leadership = new[] { new { name = "PI", required = true, staffId = newStaff.Id } }
        };
        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}", payload);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var leadership = doc.RootElement.GetProperty("leadership").EnumerateArray().Single();
        Assert.Equal(newStaff.Id, leadership.GetProperty("staff").GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task PutStudy_ReplacesContactsWholesale_OmittedSlotsCleared()
    {
        var studyId = await StudySeedHelpers.SeedFullStudyAsync(_factory, "Put Study");
        var sponsorTeamId = await GetSponsorTeamIdAsync(studyId);
        var client = _factory.CreateClient();

        // Full study has one Irb/slot1 contact seeded; PUT with a different contact only.
        var payload = new
        {
            sponsorTeamId,
            name = "Put Study Updated",
            status = "Enrolling",
            contacts = new[] { new { type = "Cro", slot = 1, name = "New CRO" } }
        };

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}", payload);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var contacts = doc.RootElement.GetProperty("contacts").EnumerateArray().ToList();
        Assert.Single(contacts);
        Assert.Equal("Cro", contacts[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task PatchStudy_UpsertsContactSlot_LeavesOtherSlotsUntouched()
    {
        var studyId = await StudySeedHelpers.SeedFullStudyAsync(_factory, "Patch Study");
        var client = _factory.CreateClient();

        var payload = new { contacts = new[] { new { type = "Cro", slot = 1, name = "Added CRO" } } };
        var resp = await client.PatchAsJsonAsync($"/api/v1/studies/{studyId}", payload);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var contacts = doc.RootElement.GetProperty("contacts").EnumerateArray().ToList();
        Assert.Equal(2, contacts.Count); // original Irb/1 + new Cro/1
        Assert.Contains(contacts, c => c.GetProperty("type").GetString() == "Irb");
        Assert.Contains(contacts, c => c.GetProperty("type").GetString() == "Cro");
    }

    [Fact]
    public async Task PatchStudy_Returns400_WhenSponsorTeamIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Patch Invalid Sponsor");
        var client = _factory.CreateClient();

        var resp = await client.PatchAsJsonAsync($"/api/v1/studies/{studyId}", new { sponsorTeamId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteStudy_CascadesToSubResourcesIncludingContacts()
    {
        var studyId = await StudySeedHelpers.SeedFullStudyAsync(_factory, "Delete Study");
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/studies/{studyId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(db.StudyContacts.Where(c => c.StudyId == studyId));
        Assert.Empty(db.StudyTargetDates.Where(t => t.StudyId == studyId));
    }

    [Fact]
    public async Task DeleteStudy_Returns404_WhenMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.DeleteAsync("/api/v1/studies/900000003");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    private async Task<(int SponsorTeamId, int SiteId, int StaffId)> SeedPrerequisitesAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await StudySeedHelpers.SeedPrerequisitesAsync(db);
    }

    private async Task<int> GetSponsorTeamIdAsync(int studyId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var study = await db.Studies.FindAsync(studyId);
        return study!.SponsorTeamId;
    }
}
