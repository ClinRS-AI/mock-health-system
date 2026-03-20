namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientMedicationCondition
{
    public int PatientMedicationId { get; set; }
    public int PatientConditionId { get; set; }

    public PatientMedication PatientMedication { get; set; } = null!;
    public PatientCondition PatientCondition { get; set; } = null!;
}
