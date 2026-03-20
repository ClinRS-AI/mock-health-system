namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientSocialHistoryEntry
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int SocialHistoryId { get; set; }
    public string? Comment { get; set; }

    public Patient Patient { get; set; } = null!;
    public SocialHistory SocialHistory { get; set; } = null!;
}
