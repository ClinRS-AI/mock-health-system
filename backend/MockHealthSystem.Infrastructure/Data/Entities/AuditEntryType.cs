namespace MockHealthSystem.Infrastructure.Data.Entities;

public class AuditEntryType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
