namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientPhone
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int Slot { get; set; }
    public string? Number { get; set; }
    public string? RawNumber { get; set; }
    public bool OutOfService { get; set; }

    public Patient Patient { get; set; } = null!;
}
