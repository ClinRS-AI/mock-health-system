namespace MockHealthSystem.Api.Models.Patients;

public class PatientAllergiesViewModel
{
    public int Id { get; set; }
    public string? Reaction { get; set; }
    public string? Comment { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PatientAllergyViewModel? Allergy { get; set; }
}
