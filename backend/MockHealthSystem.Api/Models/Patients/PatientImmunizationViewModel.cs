namespace MockHealthSystem.Api.Models.Patients;

public class PatientImmunizationViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Comment { get; set; }
    public string? Location { get; set; }
    public DateTime? Date { get; set; }
    public ImmunizationTypeViewModel? ImmunizationType { get; set; }
}
