using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Validates research.md Decision 6: resetting Patient or Study data must also remove
/// referencing Subject/SubjectStatus data (FR-012), without a separate explicit Subject reset.
///
/// The production `patients/reset`/`studies/reset`/`subjects/reset` actions use raw SQL
/// `TRUNCATE ... CASCADE`, which the EF InMemory provider does not support at all (it throws,
/// surfacing as 500 — see TestDataControllerStudyResetTests/TestDataControllerSubjectResetTests'
/// identical, already-accepted precedent). That means the TRUNCATE-based cascade itself can only
/// be verified against real PostgreSQL (T049's live-Postgres verification is the authoritative
/// check). What *is* verifiable here, and is the thing actually at risk of misconfiguration, is
/// the `OnDelete(DeleteBehavior.Cascade)` FK configuration in AppDbContext — if that were wrong
/// (e.g. accidentally Restrict/NoAction), both the real Postgres TRUNCATE CASCADE and any other
/// delete path would silently stop cascading. This class exercises that configuration directly
/// via EF's own change-tracker cascade (a different code path from the TRUNCATE endpoints, but
/// governed by the same FK configuration).
/// </summary>
public sealed class TestDataControllerSubjectCascadeTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectCascadeTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResetPatients_ReturnsInternalServerError_AgainstInMemoryProvider_WhenSubjectsExist()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);
        var client = _factory.CreateClient();

        var resp = await client.PostAsync("/api/v1/test-data/patients/reset", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    [Fact]
    public async Task DeletingPatient_CascadesToReferencingSubjectsAndTheirStatusHistory()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Load (track) the dependent Subjects and their StatusHistory first — EF's change-tracker
        // cascade only reaches entities it already has tracked, so this is what lets the
        // Patient.Remove() below cascade automatically via the OnDelete(Cascade) FK config,
        // rather than proving nothing by removing the dependents manually.
        await db.Subjects.Include(s => s.StatusHistory).Where(s => s.PatientId == patientId).ToListAsync();
        var patient = await db.Patients.FirstAsync(p => p.Id == patientId);
        db.Patients.Remove(patient);
        await db.SaveChangesAsync();

        Assert.False(await db.Subjects.AnyAsync(s => s.Id == subjectId));
        Assert.False(await db.SubjectStatuses.AnyAsync(h => h.SubjectId == subjectId));
    }

    [Fact]
    public async Task DeletingStudy_CascadesToReferencingSubjectsAndTheirStatusHistory()
    {
        var (patientId, studyId, _) = await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var subjectId = await SubjectSeedHelpers.SeedSubjectAsync(_factory, patientId, studyId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Subjects.Include(s => s.StatusHistory).Where(s => s.StudyId == studyId).ToListAsync();
        var study = await db.Studies.FirstAsync(s => s.Id == studyId);
        db.Studies.Remove(study);
        await db.SaveChangesAsync();

        Assert.False(await db.Subjects.AnyAsync(s => s.Id == subjectId));
        Assert.False(await db.SubjectStatuses.AnyAsync(h => h.SubjectId == subjectId));
    }
}
