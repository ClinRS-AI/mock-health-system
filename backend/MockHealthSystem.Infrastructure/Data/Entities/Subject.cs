namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Subject
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
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
    public DateTime CreatedOn { get; set; }
    public DateTime LastUpdatedOn { get; set; }

    public Patient Patient { get; set; } = null!;
    public Study Study { get; set; } = null!;
    public Site? Site { get; set; }
    public StudyArm? StudyArm { get; set; }
    public ProtocolVersion? ProtocolVersion { get; set; }
    public ICollection<SubjectStatus> StatusHistory { get; set; } = new List<SubjectStatus>();
}
