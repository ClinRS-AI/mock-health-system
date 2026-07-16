using MockHealthSystem.Api.Models.Patients;

namespace MockHealthSystem.Api.Models.Studies;

public class StudyVisitViewModel
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public StudyPreviewModel Study { get; set; } = null!;
    public ProtocolVersionPreviewModel? ProtocolVersion { get; set; }
    public IList<StudyArmPreviewModel> Arms { get; set; } = new List<StudyArmPreviewModel>();
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
    public bool IsActive { get; set; }
    public bool AutoRepeat { get; set; }
    public bool RepeatOnDemand { get; set; }
    public string? ImportId { get; set; }
    public string? ImportType { get; set; }
}

public class StudyVisitEditModel
{
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
}
