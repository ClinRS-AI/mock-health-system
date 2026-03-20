namespace MockHealthSystem.Api.Models.Patients;

public class PatientFamilyHistoryViewModel
{
    public int Id { get; set; }
    public string? RelationName { get; set; }
    public string? AgeAtOnset { get; set; }
    public string? Comment { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public RelationViewModel? Relation { get; set; }
    public ConditionPreviewViewModel? Condition { get; set; }
}
