namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyVisit
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int StudyId { get; set; }
    public int? ProtocolVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Reference { get; set; }
    public string? OptionalProcedure { get; set; }
    public string? Description { get; set; }
    public int? StandardMinutes { get; set; }
    public decimal? Budget { get; set; }
    public decimal? Cost { get; set; }
    public bool IsBudgetAutoRecomputed { get; set; }
    public bool IsCostAutoRecomputed { get; set; }
    public decimal? PatientStipend { get; set; }
    public decimal? CaregiverStipend { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoRepeat { get; set; }
    public bool RepeatOnDemand { get; set; }
    public string? ImportId { get; set; }
    public string? ImportType { get; set; }

    public Study Study { get; set; } = null!;
    public ProtocolVersion? ProtocolVersion { get; set; }
    public ICollection<StudyVisitArm> VisitArms { get; set; } = new List<StudyVisitArm>();
}
