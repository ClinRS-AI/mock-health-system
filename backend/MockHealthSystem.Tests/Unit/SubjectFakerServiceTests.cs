using MockHealthSystem.Api.Models.Subjects;
using MockHealthSystem.Api.Services;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class SubjectFakerServiceTests
{
    private static SubjectFakerService CreateService(
        int? seed = 42,
        IEnumerable<(int PatientId, int StudyId)>? existingActiveCategoryPairs = null,
        IReadOnlyList<int>? siteIds = null) => new(
        seed,
        patientIds: [1, 2, 3],
        studyIds: [10, 20],
        siteIds: siteIds ?? [200, 201],
        studyArmIdsByStudyId: new Dictionary<int, IReadOnlyList<int>>
        {
            [10] = [100, 101],
            [20] = []
        },
        protocolVersionIdsByStudyId: new Dictionary<int, IReadOnlyList<int>>
        {
            [10] = [300, 301],
            [20] = []
        },
        existingActiveCategoryPairs: (existingActiveCategoryPairs ?? []).ToList());

    [Fact]
    public void CreateSubjects_AlwaysHasUid()
    {
        var svc = CreateService();
        var subjects = svc.CreateSubjects(3);
        Assert.All(subjects, s => Assert.NotEqual(Guid.Empty, s.Uid));
    }

    [Fact]
    public void CreateSubjects_PatientId_ResolvesToProvidedList()
    {
        var svc = CreateService();
        var subjects = svc.CreateSubjects(3);
        Assert.All(subjects, s => Assert.Contains(s.PatientId, new[] { 1, 2, 3 }));
    }

    [Fact]
    public void CreateSubjects_StudyId_ResolvesToProvidedList()
    {
        var svc = CreateService();
        var subjects = svc.CreateSubjects(3);
        Assert.All(subjects, s => Assert.Contains(s.StudyId, new[] { 10, 20 }));
    }

    [Fact]
    public void CreateSubjects_Status_ResolvesToDefinedVocabulary()
    {
        var svc = CreateService();
        var subjects = svc.CreateSubjects(6);
        Assert.All(subjects, s => Assert.Contains(s.Status, SubjectStatusCatalog.AllStatuses));
    }

    [Fact]
    public void CreateSubjects_EachHasExactlyOneInitialStatusHistoryEntry()
    {
        var svc = CreateService();
        var subjects = svc.CreateSubjects(4);
        Assert.All(subjects, s =>
        {
            Assert.Single(s.StatusHistory);
            Assert.Equal(s.Status, s.StatusHistory.First().StatusName);
        });
    }

    [Fact]
    public void CreateSubjects_StudyArmId_WhenSet_BelongsToThatStudy()
    {
        var svc = CreateService();
        var subjects = svc.CreateSubjects(20);
        foreach (var s in subjects.Where(s => s.StudyArmId.HasValue))
        {
            Assert.Equal(10, s.StudyId); // only study 10 has arms configured
            Assert.Contains(s.StudyArmId!.Value, new[] { 100, 101 });
        }
    }

    [Fact]
    public void CreateSubjects_ProtocolVersionId_WhenSet_BelongsToThatStudy()
    {
        var svc = CreateService();
        var subjects = svc.CreateSubjects(20);
        foreach (var s in subjects.Where(s => s.ProtocolVersionId.HasValue))
        {
            Assert.Equal(10, s.StudyId); // only study 10 has protocol versions configured
            Assert.Contains(s.ProtocolVersionId!.Value, new[] { 300, 301 });
        }
    }

    [Fact]
    public void CreateSubjects_SiteId_WhenSet_ResolvesToProvidedList()
    {
        var svc = CreateService(siteIds: [200, 201]);
        var subjects = svc.CreateSubjects(20);
        foreach (var s in subjects.Where(s => s.SiteId.HasValue))
        {
            Assert.Contains(s.SiteId!.Value, new[] { 200, 201 });
        }
    }

    [Fact]
    public void CreateSubjects_SiteId_IsNull_WhenNoSitesProvided()
    {
        var svc = CreateService(siteIds: []);
        var subjects = svc.CreateSubjects(5);
        Assert.All(subjects, s => Assert.Null(s.SiteId));
    }

    [Fact]
    public void CreateSubjects_ExcludesPairsWithExistingActiveCategorySubject()
    {
        var svc = CreateService(existingActiveCategoryPairs: [(1, 10)]);
        var subjects = svc.CreateSubjects(10);
        Assert.DoesNotContain(subjects, s => s.PatientId == 1 && s.StudyId == 10);
    }

    [Fact]
    public void CreateSubjects_ReturnsFewerThanRequested_WhenFewerValidCombinationsExist()
    {
        // 3 patients x 2 studies = 6 total pairs; requesting far more than that must not throw
        // and must return no more than the number of distinct valid pairs.
        var svc = CreateService();
        var subjects = svc.CreateSubjects(50);
        Assert.True(subjects.Count <= 6);
        var distinctPairs = subjects.Select(s => (s.PatientId, s.StudyId)).Distinct().Count();
        Assert.Equal(subjects.Count, distinctPairs);
    }
}
