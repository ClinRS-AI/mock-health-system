namespace MockHealthSystem.Api.Models.Patients;

public class PatientImmunizationEditModel
{
    public int ImmunizationId { get; set; }
    public string? Location { get; set; }
    public DateTime? Date { get; set; }
    public string? Comment { get; set; }
}
