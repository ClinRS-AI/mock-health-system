namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Provider
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public int? ProviderTypeId { get; set; }

    public ProviderType? ProviderType { get; set; }
}
