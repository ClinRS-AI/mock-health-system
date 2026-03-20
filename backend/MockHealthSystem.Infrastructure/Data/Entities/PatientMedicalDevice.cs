namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientMedicalDevice
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DeviceId { get; set; }
    public string? Comment { get; set; }

    public Patient Patient { get; set; } = null!;
    public Device Device { get; set; } = null!;
}
