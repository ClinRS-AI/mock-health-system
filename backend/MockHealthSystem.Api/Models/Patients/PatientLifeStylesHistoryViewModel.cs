namespace MockHealthSystem.Api.Models.Patients;

public class PatientLifeStylesHistoryViewModel
{
    public int Id { get; set; }
    public string? Comment { get; set; }
    public PatientSocialHistoryViewModel? SocialHistory { get; set; }
}
