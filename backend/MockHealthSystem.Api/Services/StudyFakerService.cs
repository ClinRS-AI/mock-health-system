using Bogus;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Services;

/// <summary>
/// Generates realistic fake Study records (with populated structural sub-resources) using the
/// Bogus faker library. Mirrors PatientFakerService's shape: seed + prerequisite lookup IDs
/// resolved by the caller before construction. See research.md.
/// </summary>
public sealed class StudyFakerService
{
    private static readonly string[] Phases = { "Phase I", "Phase I/II", "Phase II", "Phase II/III", "Phase III", "Phase IV" };
    private static readonly string[] ArmStatuses = { "Active", "Closed", "Suspended" };
    private static readonly string[] VisitTypes = { "Screening", "Treatment", "Follow-up", "Unscheduled" };
    private static readonly string[] MilestoneCategories = { "Regulatory", "Enrollment", "Financial", "Operational" };
    private static readonly string[] MilestoneImportance = { "Low", "Medium", "High", "Critical" };
    private static readonly string[] MilestoneStatuses = { "Pending", "In Progress", "Complete", "Overdue" };
    private static readonly string[] DocumentTypes = { "Protocol", "Informed Consent", "IRB Approval", "Investigator Brochure", "Regulatory Binder" };
    private static readonly string[] DocumentStatuses = { "Draft", "Under Review", "Approved", "Expired" };
    private static readonly string[] ContactTypes = { "Irb", "Cro", "Lab", "Monitor", "Vendor" };

    private readonly Faker _faker;
    private readonly List<int> _sponsorTeamIds;
    private readonly List<int> _siteIds;
    private readonly List<int> _staffIds;
    private readonly List<string> _categories;
    private readonly List<string> _subcategories;
    private readonly List<string> _statuses;
    private readonly List<string> _groups;

    public StudyFakerService(
        int? seed,
        IReadOnlyList<int> sponsorTeamIds,
        IReadOnlyList<int> siteIds,
        IReadOnlyList<int> staffIds,
        IReadOnlyList<string> categories,
        IReadOnlyList<string> subcategories,
        IReadOnlyList<string> statuses,
        IReadOnlyList<string> groups)
    {
        _sponsorTeamIds = sponsorTeamIds.Count > 0 ? sponsorTeamIds.ToList() : throw new ArgumentException("At least one sponsor team is required.", nameof(sponsorTeamIds));
        _siteIds = siteIds.ToList();
        _staffIds = staffIds.ToList();
        _categories = categories.ToList();
        _subcategories = subcategories.ToList();
        _statuses = statuses.Count > 0 ? statuses.ToList() : new List<string> { "Enrolling" };
        _groups = groups.ToList();

        if (seed.HasValue)
            Randomizer.Seed = new Random(seed.Value);
        _faker = new Faker("en_US");
    }

    /// <summary>Creates a single study with faker-generated fields and populated structural sub-resources.</summary>
    public Study CreateStudy()
    {
        var sponsor = _faker.Company.CompanyName();
        var protocolNumber = $"{_faker.Random.AlphaNumeric(4).ToUpperInvariant()}-{_faker.Random.Number(100, 999)}";

        var study = new Study
        {
            Uid = _faker.Random.Guid(),
            Name = $"{sponsor} {_faker.Commerce.ProductAdjective()} Study",
            Title = _faker.Lorem.Sentence(8),
            Identifier = _faker.Random.AlphaNumeric(8).ToUpperInvariant(),
            ProtocolNumber = protocolNumber,
            IndIdeNumber = _faker.Random.Bool(0.5f) ? _faker.Random.Number(100000, 999999).ToString() : null,
            NctNumber = _faker.Random.Bool(0.7f) ? $"NCT{_faker.Random.Number(10000000, 99999999)}" : null,
            Phase = _faker.PickRandom(Phases),
            Status = _faker.PickRandom(_statuses),
            Category = _categories.Count > 0 ? _faker.PickRandom(_categories) : null,
            Subcategory = _subcategories.Count > 0 ? _faker.PickRandom(_subcategories) : null,
            StudyGroup = _groups.Count > 0 ? _faker.PickRandom(_groups) : null,
            Tag1 = _faker.Random.Bool(0.3f) ? _faker.Commerce.Department() : null,
            Comment = _faker.Random.Bool(0.3f) ? _faker.Lorem.Sentence() : null,
            Description = _faker.Lorem.Paragraph(),
            LaunchYear = _faker.Date.Past(5).Year,
            StudyCurrency = "USD",
            SponsorTeamId = _faker.PickRandom(_sponsorTeamIds),
            ManagingSiteId = _siteIds.Count > 0 && _faker.Random.Bool(0.8f) ? _faker.PickRandom(_siteIds) : null,
            FinanceType = _faker.PickRandom("Fixed", "Milestone-Based", "Per-Patient"),
            AccountingCode1 = _faker.Random.AlphaNumeric(6).ToUpperInvariant(),
            OpportunityLevel = _faker.PickRandom("Low", "Medium", "High"),
            OpportunityProbability = Math.Round(_faker.Random.Double(0, 1), 2),
            OpportunityExpectedDate = Utc(_faker.Date.Future(1)),
            OpportunityExpectedNumberOfSites = _faker.Random.Number(1, 50),
            EnrollmentNote = _faker.Random.Bool(0.3f) ? _faker.Lorem.Sentence() : null,
            LeadSourceStaffId = PickStaffOrNull(),
            LeadSource = _faker.PickRandom("Referral", "Conference", "Cold Outreach", "Repeat Sponsor"),
            LeadDate = Utc(_faker.Date.Past(1)),
            CreatedOn = DateTime.UtcNow,
            LastUpdatedOn = DateTime.UtcNow
        };

        study.TargetDates.Add(new StudyTargetDate { Name = "First Patient In", Required = true, TargetDate = Utc(_faker.Date.Future(1)) });
        study.TargetDates.Add(new StudyTargetDate { Name = "Last Patient Out", Required = false, TargetDate = Utc(_faker.Date.Future(2)) });

        var leaderStaffId = PickStaffOrNull();
        if (leaderStaffId.HasValue)
            study.Leadership.Add(new StudyLeadership { Name = "Principal Investigator", Required = true, StaffId = leaderStaffId });

        study.CustomFieldValues.Add(new StudyCustomFieldValue { FieldName = "Therapeutic Area", FieldValue = _faker.Commerce.Department() });

        var allContactSlots = ContactTypes.SelectMany(t => new[] { (Type: t, Slot: 1), (Type: t, Slot: 2) });
        var pickedContactSlots = _faker.PickRandom(allContactSlots, _faker.Random.Int(1, 3));
        foreach (var (type, slot) in pickedContactSlots)
            study.Contacts.Add(new StudyContact { ContactType = type, Slot = slot, Name = _faker.Company.CompanyName(), Reference = _faker.Random.AlphaNumeric(6) });

        var arms = CreateArms(_faker.Random.Int(1, 3));
        foreach (var arm in arms) study.Arms.Add(arm);

        var visits = CreateVisits(_faker.Random.Int(2, 5));
        foreach (var visit in visits)
        {
            study.Visits.Add(visit);
            foreach (var arm in arms)
            {
                if (_faker.Random.Bool(0.7f))
                    visit.VisitArms.Add(new StudyVisitArm { StudyVisit = visit, StudyArm = arm });
            }
        }

        foreach (var milestone in CreateMilestones(_faker.Random.Int(2, 4)))
            study.Milestones.Add(milestone);

        foreach (var document in CreateDocuments(_faker.Random.Int(1, 3)))
            study.Documents.Add(document);

        foreach (var note in CreateNotes(_faker.Random.Int(0, 3)))
            study.Notes.Add(note);

        return study;
    }

    /// <summary>Creates multiple studies, each with populated structural sub-resources.</summary>
    public IReadOnlyList<Study> CreateStudies(int count)
    {
        var list = new List<Study>(count);
        for (var i = 0; i < count; i++) list.Add(CreateStudy());
        return list;
    }

    private int? PickStaffOrNull() => _staffIds.Count > 0 ? _faker.PickRandom(_staffIds) : null;

    /// <summary>Bogus's Date.Past/Future return Kind=Local; Npgsql rejects non-UTC DateTimes for
    /// timestamptz columns. Mirrors PatientsController.ToUtc.</summary>
    private static DateTime Utc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value.ToUniversalTime()
    };

    private List<StudyArm> CreateArms(int count)
    {
        var list = new List<StudyArm>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new StudyArm
            {
                Uid = _faker.Random.Guid(),
                Name = $"Arm {(char)('A' + i)}",
                Status = _faker.PickRandom(ArmStatuses),
                PatientGoal = _faker.Random.Number(10, 200),
                PatientLimit = _faker.Random.Number(200, 400)
            });
        }
        return list;
    }

    private List<StudyVisit> CreateVisits(int count)
    {
        var list = new List<StudyVisit>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new StudyVisit
            {
                Uid = _faker.Random.Guid(),
                Name = $"Visit {i + 1}",
                Type = _faker.PickRandom(VisitTypes),
                StandardMinutes = _faker.Random.Number(15, 120),
                Budget = _faker.Random.Decimal(100, 5000),
                Cost = _faker.Random.Decimal(50, 2500),
                PatientStipend = _faker.Random.Bool(0.6f) ? _faker.Random.Decimal(10, 200) : null,
                IsActive = true,
                AutoRepeat = _faker.Random.Bool(0.1f)
            });
        }
        return list;
    }

    private List<StudyMilestone> CreateMilestones(int count)
    {
        var list = new List<StudyMilestone>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new StudyMilestone
            {
                Name = _faker.Commerce.ProductName(),
                Category = _faker.PickRandom(MilestoneCategories),
                Importance = _faker.PickRandom(MilestoneImportance),
                Status = _faker.PickRandom(MilestoneStatuses),
                AssignedToStaffId = PickStaffOrNull(),
                ProjectedDate = Utc(_faker.Date.Future(1))
            });
        }
        return list;
    }

    private List<StudyDocument> CreateDocuments(int count)
    {
        var list = new List<StudyDocument>(count);
        for (var i = 0; i < count; i++)
        {
            var status = _faker.PickRandom(DocumentStatuses);
            var document = new StudyDocument
            {
                Uid = _faker.Random.Guid(),
                TypeName = _faker.PickRandom(DocumentTypes),
                StatusName = status,
                Version = $"v{_faker.Random.Number(1, 5)}.{_faker.Random.Number(0, 9)}",
                EffectiveDate = Utc(_faker.Date.Past(1))
            };
            document.StatusHistory.Add(new StudyDocumentStatusHistory { StatusName = status, ChangedOn = DateTime.UtcNow });
            list.Add(document);
        }
        return list;
    }

    private List<StudyNote> CreateNotes(int count)
    {
        var list = new List<StudyNote>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new StudyNote
            {
                StaffId = PickStaffOrNull(),
                NoteDate = Utc(_faker.Date.Past(1)),
                Note = _faker.Lorem.Sentence(),
                Shared = _faker.Random.Bool(0.7f)
            });
        }
        return list;
    }
}
