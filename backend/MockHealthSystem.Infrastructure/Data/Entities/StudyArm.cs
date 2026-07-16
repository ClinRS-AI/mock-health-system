namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyArm
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int StudyId { get; set; }
    public int? ProtocolVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int? PatientGoal { get; set; }
    public int? PatientLimit { get; set; }
    public string? Comment { get; set; }
    public string? ImportId { get; set; }
    public string? ImportType { get; set; }

    public Study Study { get; set; } = null!;
    public ProtocolVersion? ProtocolVersion { get; set; }
    public ICollection<StudyVisitArm> VisitArms { get; set; } = new List<StudyVisitArm>();
}
