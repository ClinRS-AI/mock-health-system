namespace MockHealthSystem.Api.Models.Patients;

public class PatientConditionViewModel
{
    public int Id { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AgeAtOnset { get; set; }
    public string? Comment { get; set; }
    public ConditionPreviewViewModel? Condition { get; set; }
}
