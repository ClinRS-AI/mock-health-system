namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientCondition
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int ConditionId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AgeAtOnset { get; set; }
    public string? Comment { get; set; }

    public Patient Patient { get; set; } = null!;
    public Condition Condition { get; set; } = null!;
    public ICollection<PatientMedicationCondition> MedicationConditions { get; set; } = new List<PatientMedicationCondition>();
}
