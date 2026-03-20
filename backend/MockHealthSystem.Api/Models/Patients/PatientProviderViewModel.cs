namespace MockHealthSystem.Api.Models.Patients;

public class PatientProviderViewModel
{
    public int Id { get; set; }
    public string? Comment { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProviderViewModel? Provider { get; set; }
}
