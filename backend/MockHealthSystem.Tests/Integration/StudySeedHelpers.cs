using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Tests.Integration;

/// <summary>Shared seed helpers for Study-domain integration tests.</summary>
internal static class StudySeedHelpers
{
    public static async Task<(int SponsorTeamId, int SiteId, int StaffId)> SeedPrerequisitesAsync(AppDbContext db)
    {
        var sponsor = new Sponsor { Uid = Guid.NewGuid(), Name = $"Sponsor-{Guid.NewGuid():N}" };
        db.Sponsors.Add(sponsor);
        await db.SaveChangesAsync();

        var division = new SponsorDivision { SponsorId = sponsor.Id, Name = "Division" };
        db.SponsorDivisions.Add(division);
        await db.SaveChangesAsync();

        var team = new SponsorTeam { SponsorDivisionId = division.Id, Name = "Team" };
        db.SponsorTeams.Add(team);

        var site = new Site { Uid = Guid.NewGuid(), Name = "Site" };
        db.Sites.Add(site);

        var staff = new Staff { StaffUid = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", IsActive = true };
        db.Staff.Add(staff);

        await db.SaveChangesAsync();
        return (team.Id, site.Id, staff.Id);
    }

    public static async Task<int> SeedStudyAsync(IsolatedWebApplicationFactory factory, string name = "Test Study", Action<Study>? configure = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (sponsorTeamId, siteId, _) = await SeedPrerequisitesAsync(db);

        var study = new Study
        {
            Uid = Guid.NewGuid(),
            Name = name,
            Status = "Enrolling",
            SponsorTeamId = sponsorTeamId,
            ManagingSiteId = siteId,
            CreatedOn = DateTime.UtcNow,
            LastUpdatedOn = DateTime.UtcNow
        };
        configure?.Invoke(study);

        db.Studies.Add(study);
        await db.SaveChangesAsync();
        return study.Id;
    }

    public static async Task<int> SeedFullStudyAsync(IsolatedWebApplicationFactory factory, string name = "Full Study")
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (sponsorTeamId, siteId, staffId) = await SeedPrerequisitesAsync(db);

        var study = new Study
        {
            Uid = Guid.NewGuid(),
            Name = name,
            Status = "Enrolling",
            SponsorTeamId = sponsorTeamId,
            ManagingSiteId = siteId,
            FinanceType = "Fixed",
            OpportunityLevel = "High",
            CreatedOn = DateTime.UtcNow,
            LastUpdatedOn = DateTime.UtcNow
        };
        study.TargetDates.Add(new StudyTargetDate { Name = "FPI", Required = true });
        study.Leadership.Add(new StudyLeadership { Name = "Principal Investigator", Required = true, StaffId = staffId });
        study.CustomFieldValues.Add(new StudyCustomFieldValue { FieldName = "Notes", FieldValue = "Test" });
        study.Contacts.Add(new StudyContact { ContactType = "Irb", Slot = 1, Name = "Central IRB" });

        db.Studies.Add(study);
        await db.SaveChangesAsync();
        return study.Id;
    }
}
