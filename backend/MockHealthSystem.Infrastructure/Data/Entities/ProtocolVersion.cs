namespace MockHealthSystem.Infrastructure.Data.Entities;

public class ProtocolVersion
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int StudyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? VersionDate { get; set; }
    public string? TreatmentStatus { get; set; }
    public string? Status { get; set; }
    public string? ProtocolNumber { get; set; }
    public string? Comment { get; set; }
    public DateTime? IrbApprovalDate { get; set; }
    public bool IsPatientReconsentRequired { get; set; }
    public string? ImportId { get; set; }
    public string? ImportType { get; set; }

    public Study Study { get; set; } = null!;
}
