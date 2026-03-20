namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientProvider
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int ProviderId { get; set; }
    public string? Comment { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Patient Patient { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
}
