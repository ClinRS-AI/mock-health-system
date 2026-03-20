namespace MockHealthSystem.Api.Models.Patients;

public class PatientMedicationEditModel
{
    public int Id { get; set; }
    public string? Dosage { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Comment { get; set; }
    public int RouteId { get; set; }
    public IList<PatientMedicationConditionEditModel>? Conditions { get; set; }
}
