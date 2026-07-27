using Bogus;
using MockHealthSystem.Api.Models.Subjects;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Services;

/// <summary>
/// Generates realistic fake Subject records (with an initial SubjectStatus row each) using the
/// Bogus faker library, linking only existing Patients and Studies. Mirrors StudyFakerService's
/// shape: seed + prerequisite lookup IDs resolved by the caller before construction.
///
/// Candidate (PatientId, StudyId) pairs are picked via bounded random sampling rather than
/// materializing the full cross product up front — this project has been seen with 100k+
/// patients in local testing, so an eagerly-built Patients x Studies cross product would not
/// scale. See research.md Decision 7.
/// </summary>
public sealed class SubjectFakerService
{
    private const int MaxAttemptsPerRequestedSubject = 20;

    private static readonly string[] GenderCodes = { "M", "F", "U" };

    private readonly Faker _faker;
    private readonly List<int> _patientIds;
    private readonly List<int> _studyIds;
    private readonly List<int> _siteIds;
    private readonly Dictionary<int, List<int>> _studyArmIdsByStudyId;
    private readonly Dictionary<int, List<int>> _protocolVersionIdsByStudyId;
    private readonly HashSet<(int PatientId, int StudyId)> _blockedPairs;
    private static readonly List<string> AllStatuses = SubjectStatusCatalog.AllStatuses.ToList();

    public SubjectFakerService(
        int? seed,
        IReadOnlyList<int> patientIds,
        IReadOnlyList<int> studyIds,
        IReadOnlyList<int> siteIds,
        IReadOnlyDictionary<int, IReadOnlyList<int>> studyArmIdsByStudyId,
        IReadOnlyDictionary<int, IReadOnlyList<int>> protocolVersionIdsByStudyId,
        IReadOnlyCollection<(int PatientId, int StudyId)> existingActiveCategoryPairs)
    {
        _patientIds = patientIds.Count > 0 ? patientIds.ToList() : throw new ArgumentException("At least one patient is required.", nameof(patientIds));
        _studyIds = studyIds.Count > 0 ? studyIds.ToList() : throw new ArgumentException("At least one study is required.", nameof(studyIds));
        _siteIds = siteIds.ToList();
        _studyArmIdsByStudyId = studyArmIdsByStudyId.ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
        _protocolVersionIdsByStudyId = protocolVersionIdsByStudyId.ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
        _blockedPairs = existingActiveCategoryPairs.ToHashSet();

        if (seed.HasValue)
            Randomizer.Seed = new Random(seed.Value);
        _faker = new Faker("en_US");
    }

    /// <summary>Creates up to <paramref name="count"/> subjects linking distinct (Patient, Study)
    /// pairs that don't already have an Active-category subject. Returns fewer than requested if
    /// the candidate space is exhausted (spec.md edge case) — bounded random sampling means this
    /// is a best-effort search, not an exhaustive one, which is an acceptable tradeoff for
    /// synthetic test data.</summary>
    public IReadOnlyList<Subject> CreateSubjects(int count)
    {
        var picked = new HashSet<(int PatientId, int StudyId)>();
        var subjects = new List<Subject>(count);
        var maxAttempts = Math.Max(count * MaxAttemptsPerRequestedSubject, 100);

        for (var attempt = 0; subjects.Count < count && attempt < maxAttempts; attempt++)
        {
            var patientId = _faker.PickRandom(_patientIds);
            var studyId = _faker.PickRandom(_studyIds);
            var pair = (patientId, studyId);

            if (_blockedPairs.Contains(pair) || !picked.Add(pair))
                continue;

            subjects.Add(CreateSubject(patientId, studyId));
        }

        return subjects;
    }

    private Subject CreateSubject(int patientId, int studyId)
    {
        var status = _faker.PickRandom(AllStatuses);

        var arms = _studyArmIdsByStudyId.TryGetValue(studyId, out var studyArms) ? studyArms : new List<int>();
        var studyArmId = arms.Count > 0 && _faker.Random.Bool(0.6f) ? _faker.PickRandom(arms) : (int?)null;

        var protocolVersions = _protocolVersionIdsByStudyId.TryGetValue(studyId, out var studyProtocolVersions) ? studyProtocolVersions : new List<int>();
        var protocolVersionId = protocolVersions.Count > 0 && _faker.Random.Bool(0.5f) ? _faker.PickRandom(protocolVersions) : (int?)null;

        var siteId = _siteIds.Count > 0 && _faker.Random.Bool(0.7f) ? _faker.PickRandom(_siteIds) : (int?)null;

        var subject = new Subject
        {
            Uid = _faker.Random.Guid(),
            PatientId = patientId,
            StudyId = studyId,
            SiteId = siteId,
            StudyArmId = studyArmId,
            ProtocolVersionId = protocolVersionId,
            Status = status,
            GenderCode = _faker.PickRandom(GenderCodes),
            Race = _faker.Random.Bool(0.6f) ? _faker.PickRandom("White", "Black or African American", "Asian", "American Indian or Alaska Native", "Native Hawaiian or Other Pacific Islander") : null,
            Ethnicity = _faker.Random.Bool(0.6f) ? _faker.PickRandom("Hispanic or Latino", "Not Hispanic or Latino") : null,
            ImportId = _faker.Random.Bool(0.2f) ? _faker.Random.AlphaNumeric(10) : null,
            Tag = _faker.Random.Bool(0.3f) ? _faker.Commerce.Department() : null,
            FacilityCode = _faker.Random.Bool(0.4f) ? _faker.Random.AlphaNumeric(6).ToUpperInvariant() : null,
            EnrollmentDate = Utc(_faker.Date.Past(1)),
            EnrollmentLocation = _faker.Random.Bool(0.4f) ? _faker.Address.City() : null,
            ScreeningNumber = _faker.Random.Bool(0.7f) ? $"SCR-{_faker.Random.Number(1000, 9999)}" : null,
            RandomizationNumber = _faker.Random.Bool(0.4f) ? $"RND-{_faker.Random.Number(1000, 9999)}" : null,
            TreatmentStatus = _faker.Random.Bool(0.3f) ? _faker.PickRandom("On Treatment", "Off Treatment", "Discontinued") : null,
            TreatmentStart = _faker.Random.Bool(0.4f) ? Utc(_faker.Date.Past(1)) : null,
            Narrative = _faker.Random.Bool(0.2f) ? _faker.Lorem.Sentence() : null,
            CreatedOn = DateTime.UtcNow,
            LastUpdatedOn = DateTime.UtcNow
        };

        subject.StatusHistory.Add(new SubjectStatus { StatusName = status, ChangedOn = DateTime.UtcNow });
        return subject;
    }

    /// <summary>Bogus's Date.Past returns Kind=Local; Npgsql rejects non-UTC DateTimes for
    /// timestamptz columns. Mirrors StudyFakerService.Utc.</summary>
    private static DateTime Utc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value.ToUniversalTime()
    };
}
