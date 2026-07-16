using MockHealthSystem.Api.Models.Patients;

namespace MockHealthSystem.Api.Models.Studies;

public class StudyArmViewModel
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public StudyPreviewModel Study { get; set; } = null!;
    public ProtocolVersionPreviewModel? ProtocolVersion { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int? PatientGoal { get; set; }
    public int? PatientLimit { get; set; }
    public string? Comment { get; set; }
    public string? ImportId { get; set; }
    public string? ImportType { get; set; }
}

public class StudyArmEditModel
{
    public int? ProtocolVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int? PatientGoal { get; set; }
    public int? PatientLimit { get; set; }
    public string? Comment { get; set; }
    public string? ImportId { get; set; }
    public string? ImportType { get; set; }
}

public class StudyArmPreviewModel
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string? Name { get; set; }
}
