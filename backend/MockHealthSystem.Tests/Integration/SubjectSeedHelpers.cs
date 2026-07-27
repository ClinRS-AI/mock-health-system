using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Tests.Integration;

/// <summary>Shared seed helpers for Subject-domain integration tests.</summary>
internal static class SubjectSeedHelpers
{
    public static async Task<(int PatientId, int StudyId, int StudyArmId)> SeedPrerequisitesAsync(
        IsolatedWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var patient = new Patient
        {
            Uid = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Patient",
            Status = "Active"
        };
        db.Patients.Add(patient);

        var (sponsorTeamId, siteId, _) = await StudySeedHelpers.SeedPrerequisitesAsync(db);
        var study = new Study
        {
            Uid = Guid.NewGuid(),
            Name = "Test Study",
            Status = "Enrolling",
            SponsorTeamId = sponsorTeamId,
            ManagingSiteId = siteId,
            CreatedOn = DateTime.UtcNow,
            LastUpdatedOn = DateTime.UtcNow
        };
        db.Studies.Add(study);
        await db.SaveChangesAsync();

        var arm = new StudyArm { Uid = Guid.NewGuid(), StudyId = study.Id, Name = "Arm A" };
        db.StudyArms.Add(arm);
        await db.SaveChangesAsync();

        return (patient.Id, study.Id, arm.Id);
    }

    /// <summary>Seeds a Subject (plus its initial SubjectStatus row) referencing the given
    /// Patient/Study. Defaults to a non-Active-category status so callers exercising the
    /// one-Active-category-per-pair rule must opt in explicitly via <paramref name="status"/>.</summary>
    public static async Task<int> SeedSubjectAsync(
        IsolatedWebApplicationFactory factory,
        int patientId,
        int studyId,
        string status = "Prescreened",
        int? studyArmId = null,
        Action<Subject>? configure = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var subject = new Subject
        {
            Uid = Guid.NewGuid(),
            PatientId = patientId,
            StudyId = studyId,
            StudyArmId = studyArmId,
            Status = status,
            EnrollmentDate = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow,
            LastUpdatedOn = DateTime.UtcNow
        };
        configure?.Invoke(subject);

        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        db.SubjectStatuses.Add(new SubjectStatus { SubjectId = subject.Id, StatusName = subject.Status, ChangedOn = DateTime.UtcNow });
        await db.SaveChangesAsync();

        return subject.Id;
    }
}
