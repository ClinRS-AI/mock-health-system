namespace MockHealthSystem.Api.Models.Patients;

public class PatientStatusEditModel
{
    public string Status { get; set; } = "Active";
    public string Reason { get; set; } = string.Empty;
}
