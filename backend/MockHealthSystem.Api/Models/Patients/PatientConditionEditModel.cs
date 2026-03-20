namespace MockHealthSystem.Api.Models.Patients;

public class PatientConditionEditModel
{
    public int ConditionId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double? AgeAtOnset { get; set; }
    public string? Comment { get; set; }
}
