namespace MockHealthSystem.Api.Models.Patients;

public class PatientMedicationViewModel
{
    public int Id { get; set; }
    public string? Dosage { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Comment { get; set; }
    public MedicationViewModel? Medication { get; set; }
    public MedicationRouteViewModel? Route { get; set; }
    public IList<ConditionPreviewViewModel>? Conditions { get; set; }
}
