using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class StudyRolesControllerWriteTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudyRolesControllerWriteTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateRoleStaff_ReplacesAssignedStaffSet()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Role Write Study");
        var roleId = await SeedRoleAsync(studyId, "Coordinator");
        var staff1 = await SeedStaffAsync("Alice", "Coord");
        var staff2 = await SeedStaffAsync("Bob", "Backup");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/roles/{roleId}",
            new[] { new { staffId = staff1, priority = "Primary" }, new { staffId = staff2, priority = "Secondary" } });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("staff").GetArrayLength());
    }

    [Fact]
    public async Task UpdateRoleStaff_Returns400_WhenStaffIdInvalid()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Role Invalid Staff Study");
        var roleId = await SeedRoleAsync(studyId, "Coordinator");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/roles/{roleId}",
            new[] { new { staffId = 999999, priority = (string?)null } });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateRoleStaff_Returns400_WhenStaffIdDuplicatedInRequestBody()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Role Duplicate Staff Study");
        var roleId = await SeedRoleAsync(studyId, "Coordinator");
        var staff1 = await SeedStaffAsync("Alice", "Coord");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/roles/{roleId}",
            new[] { new { staffId = staff1, priority = "Primary" }, new { staffId = staff1, priority = "Secondary" } });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private async Task<int> SeedRoleAsync(int studyId, string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var role = new StudyRole { StudyId = studyId, Name = name };
        db.StudyRoles.Add(role);
        await db.SaveChangesAsync();
        return role.Id;
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
}
