using MockHealthSystem.Api.Services;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class StudyFakerServiceTests
{
    private static StudyFakerService CreateService(int? seed = 42) => new(
        seed,
        sponsorTeamIds: [1, 2, 3],
        siteIds: [10, 20],
        staffIds: [100, 200],
        categories: ["Oncology", "Cardiology"],
        subcategories: ["Solid Tumor"],
        statuses: ["Enrolling", "Closed"],
        groups: ["Group A"]);

    [Fact]
    public void CreateStudy_AlwaysHasName()
    {
        var svc = CreateService();
        var study = svc.CreateStudy();
        Assert.False(string.IsNullOrWhiteSpace(study.Name));
    }

    [Fact]
    public void CreateStudy_AlwaysHasUid()
    {
        var svc = CreateService();
        var study = svc.CreateStudy();
        Assert.NotEqual(Guid.Empty, study.Uid);
    }

    [Fact]
    public void CreateStudy_SponsorTeamId_ResolvesToProvidedList()
    {
        var svc = CreateService();
        var study = svc.CreateStudy();
        Assert.Contains(study.SponsorTeamId, new[] { 1, 2, 3 });
    }

    [Fact]
    public void CreateStudy_ManagingSiteId_WhenSet_ResolvesToProvidedList()
    {
        var svc = CreateService();
        var study = svc.CreateStudy();
        if (study.ManagingSiteId.HasValue)
            Assert.Contains(study.ManagingSiteId.Value, new[] { 10, 20 });
    }

    [Fact]
    public void CreateStudy_Status_ResolvesToProvidedList()
    {
        var svc = CreateService();
        var study = svc.CreateStudy();
        Assert.Contains(study.Status, new[] { "Enrolling", "Closed" });
    }

    [Fact]
    public void CreateStudy_PopulatesArmsVisitsMilestonesDocumentsNotes()
    {
        var svc = CreateService();
        var study = svc.CreateStudy();

        Assert.NotEmpty(study.Arms);
        Assert.NotEmpty(study.Visits);
        Assert.NotEmpty(study.Milestones);
        Assert.NotEmpty(study.Documents);
        Assert.All(study.Documents, d => Assert.NotEmpty(d.StatusHistory));
    }

    [Fact]
    public void CreateStudy_ThrowsWhenNoSponsorTeamsProvided()
    {
        Assert.Throws<ArgumentException>(() => new StudyFakerService(
            42, sponsorTeamIds: [], siteIds: [], staffIds: [], categories: [], subcategories: [], statuses: [], groups: []));
    }

    [Fact]
    public void CreateStudies_ReturnsRequestedCount()
    {
        var svc = CreateService();
        var studies = svc.CreateStudies(5);
        Assert.Equal(5, studies.Count);
    }

    [Fact]
    public void CreateStudies_ContactsCanReachSlotTwo()
    {
        var svc = CreateService(seed: null);
        var studies = svc.CreateStudies(200);

        Assert.Contains(studies, s => s.Contacts.Any(c => c.Slot == 2));
    }

    [Fact]
    public void CreateStudy_ContactsHaveNoDuplicateTypeAndSlot()
    {
        var svc = CreateService(seed: null);
        foreach (var study in svc.CreateStudies(50))
        {
            var pairs = study.Contacts.Select(c => (c.ContactType, c.Slot)).ToList();
            Assert.Equal(pairs.Count, pairs.Distinct().Count());
            Assert.All(study.Contacts, c => Assert.InRange(c.Slot, 1, 2));
        }
    }

    [Fact]
    public void CreateStudy_WithSameSeed_ProducesDeterministicOutput()
    {
        // Bogus's Randomizer.Seed is process-global: two instances alive at once would interleave
        // the same stream rather than each getting an independent one. The real guarantee this
        // mirrors (matching GeneratePatientsRequest.Seed / PatientFakerService) is that a single
        // seeded instance, fully used before the next is constructed, is reproducible run-to-run —
        // exactly how TestDataController uses it (one instance per generate request).
        var study1 = CreateService(seed: 123).CreateStudy();
        var study2 = CreateService(seed: 123).CreateStudy();

        Assert.Equal(study1.Name, study2.Name);
        Assert.Equal(study1.ProtocolNumber, study2.ProtocolNumber);
    }
}
