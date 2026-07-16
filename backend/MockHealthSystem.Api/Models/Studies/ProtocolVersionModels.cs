using MockHealthSystem.Api.Models.Patients;

namespace MockHealthSystem.Api.Models.Studies;

public class ProtocolVersionViewModel
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public StudyPreviewModel Study { get; set; } = null!;
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
}

public class ProtocolVersionEditModel
{
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
}

public class ProtocolVersionPreviewModel
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string? Name { get; set; }
}
