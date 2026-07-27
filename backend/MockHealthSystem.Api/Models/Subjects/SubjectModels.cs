using MockHealthSystem.Api.Models.Patients;
using MockHealthSystem.Api.Models.Studies;

namespace MockHealthSystem.Api.Models.Subjects;

public class SubjectViewModel
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public StudyPreviewModel Study { get; set; } = null!;
    public SitePreviewModel? Site { get; set; }
    public SubjectPatientPreviewModel Patient { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public ProtocolVersionPreviewModel? ProtocolVersion { get; set; }
    public string? GenderCode { get; set; }
    public string? Race { get; set; }
    public string? Ethnicity { get; set; }
    public StudyArmPreviewModel? Arm { get; set; }
    public string? ImportId { get; set; }
    public string? Tag { get; set; }
    public string? FacilityCode { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public string? EnrollmentLocation { get; set; }
    public string? ScreeningNumber { get; set; }
    public string? RandomizationNumber { get; set; }
    public string? TreatmentStatus { get; set; }
    public DateTime? TreatmentStart { get; set; }
    public string? Narrative { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime LastUpdatedOn { get; set; }
}

/// <summary>Richer patient preview specific to the Subject endpoint — CC's real Subject response
/// embeds these demographic fields on the patient object, unlike the plain id/uid/name previews
/// used elsewhere (StudyPreviewModel, SitePreviewModel, etc.).</summary>
public class SubjectPatientPreviewModel
{
    public int Id { get; set; }
    public Guid? Uid { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? GenderCode { get; set; }
    public string? Race { get; set; }
    public string? Ethnicity { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Fields settable on <c>POST /subjects</c> (creation only). CC's real Subject POST
/// body does not accept <c>status</c>, <c>studyArmId</c>, <c>protocolVersionId</c>, or
/// <c>genderCode</c>/<c>race</c>/<c>ethnicity</c> — those are assigned a default (status) or set
/// later via PUT/PATCH (the rest). See research.md Decision 13.</summary>
public class SubjectCreateModel
{
    public int PatientId { get; set; }
    public int StudyId { get; set; }
    public int? SiteId { get; set; }
    public string? ImportId { get; set; }
    public string? Tag { get; set; }
    public string? FacilityCode { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public string? EnrollmentLocation { get; set; }
    public string? ScreeningNumber { get; set; }
    public string? RandomizationNumber { get; set; }
    public string? TreatmentStatus { get; set; }
    public DateTime? TreatmentStart { get; set; }
    public string? Narrative { get; set; }
}

/// <summary>Fields settable on <c>PUT /subjects/{id}</c> (full update). Unlike
/// <see cref="SubjectCreateModel"/>, PUT can set <c>status</c>/<c>studyArmId</c>/
/// <c>protocolVersionId</c>/demographics — those transition after creation, not at it.</summary>
public class SubjectEditModel
{
    public int PatientId { get; set; }
    public int StudyId { get; set; }
    public int? SiteId { get; set; }
    public int? StudyArmId { get; set; }
    public int? ProtocolVersionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? GenderCode { get; set; }
    public string? Race { get; set; }
    public string? Ethnicity { get; set; }
    public string? ImportId { get; set; }
    public string? Tag { get; set; }
    public string? FacilityCode { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public string? EnrollmentLocation { get; set; }
    public string? ScreeningNumber { get; set; }
    public string? RandomizationNumber { get; set; }
    public string? TreatmentStatus { get; set; }
    public DateTime? TreatmentStart { get; set; }
    public string? Narrative { get; set; }
}

public class SubjectPatchModel
{
    public int? PatientId { get; set; }
    public int? StudyId { get; set; }
    public int? SiteId { get; set; }
    public int? StudyArmId { get; set; }
    public int? ProtocolVersionId { get; set; }
    public string? Status { get; set; }
    public string? GenderCode { get; set; }
    public string? Race { get; set; }
    public string? Ethnicity { get; set; }
    public string? ImportId { get; set; }
    public string? Tag { get; set; }
    public string? FacilityCode { get; set; }
    public DateTime? EnrollmentDate { get; set; }
    public string? EnrollmentLocation { get; set; }
    public string? ScreeningNumber { get; set; }
    public string? RandomizationNumber { get; set; }
    public string? TreatmentStatus { get; set; }
    public DateTime? TreatmentStart { get; set; }
    public string? Narrative { get; set; }
}

/// <summary>
/// CC's Subject status vocabulary — nine fixed, closed values (not admin-configurable, unlike
/// Study.Status's StudyStatusType lookup table; see research.md Decision 11). "Active" is not
/// itself one of these values — it is shorthand for "any status in ActiveStatuses" and MUST NOT
/// be checked for literally anywhere in the codebase.
/// </summary>
public static class SubjectStatusCatalog
{
    /// <summary>The status a newly created Subject is assigned — CC's POST body doesn't accept
    /// a client-specified status, so creation always starts here (research.md Decision 13).</summary>
    public const string InitialStatus = "Prescreened";

    public static readonly IReadOnlyList<string> ActiveStatuses = new[]
    {
        "Prescreened", "Screened", "Randomized", "Run-in"
    };

    public static readonly IReadOnlyList<string> InactiveStatuses = new[]
    {
        "Screen Failed", "Non Qualified", "Dropped", "Run-in Failed", "Complete"
    };

    public static readonly IReadOnlyList<string> AllStatuses = ActiveStatuses.Concat(InactiveStatuses).ToArray();

    public static bool IsActiveCategory(string status) => ActiveStatuses.Contains(status);
}
