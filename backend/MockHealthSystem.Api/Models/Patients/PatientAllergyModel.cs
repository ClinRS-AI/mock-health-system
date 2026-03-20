namespace MockHealthSystem.Api.Models.Patients;

public class PatientAllergyModel
{
    public int AllergyId { get; set; }
    public string? Reaction { get; set; }
    public string? Comment { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
