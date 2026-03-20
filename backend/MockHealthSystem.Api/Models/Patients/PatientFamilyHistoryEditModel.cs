namespace MockHealthSystem.Api.Models.Patients;

public class PatientFamilyHistoryEditModel
{
    public int ConditionId { get; set; }
    public int FamilyMemberId { get; set; }
    public double? AgeAtOnset { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Comment { get; set; }
}
