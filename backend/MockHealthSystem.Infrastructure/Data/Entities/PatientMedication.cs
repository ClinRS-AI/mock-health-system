namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientMedication
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int MedicationId { get; set; }
    public int? RouteId { get; set; }
    public string? Dosage { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Comment { get; set; }

    public Patient Patient { get; set; } = null!;
    public Medication Medication { get; set; } = null!;
    public MedicationRoute? Route { get; set; }
    public ICollection<PatientMedicationCondition> MedicationConditions { get; set; } = new List<PatientMedicationCondition>();
}
