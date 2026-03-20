namespace MockHealthSystem.Api.Models.Patients;

public class PatientMedicalDeviceViewModel
{
    public int Id { get; set; }
    public string? Comment { get; set; }
    public DeviceViewModel? Device { get; set; }
}
