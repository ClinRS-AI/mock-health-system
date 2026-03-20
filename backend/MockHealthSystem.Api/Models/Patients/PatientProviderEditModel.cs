namespace MockHealthSystem.Api.Models.Patients;

public class PatientProviderEditModel
{
    public int ProviderId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Comment { get; set; }
}
