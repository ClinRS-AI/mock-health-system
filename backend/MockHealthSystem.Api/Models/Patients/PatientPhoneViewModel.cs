namespace MockHealthSystem.Api.Models.Patients;

public class PatientPhoneViewModel
{
    public string? RawNumber { get; set; }
    public string? Number { get; set; }
    public bool OutOfService { get; set; }
}
