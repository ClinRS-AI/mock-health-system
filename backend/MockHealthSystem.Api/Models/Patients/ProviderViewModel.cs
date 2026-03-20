namespace MockHealthSystem.Api.Models.Patients;

public class ProviderViewModel
{
    public int Id { get; set; }
    public string? ProviderName { get; set; }
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public ProviderTypeViewModel? ProviderType { get; set; }
}
