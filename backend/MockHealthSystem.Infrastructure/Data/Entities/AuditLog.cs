namespace MockHealthSystem.Infrastructure.Data.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int? StaffPKey { get; set; }
    public int? PatientPKey { get; set; }
    public string? StudyPKey { get; set; }
    public DateTime CreatedTimeUtc { get; set; }
    public string CreatedByUser { get; set; } = string.Empty;
    public int AuditEntryTypeId { get; set; }
    public string? Details { get; set; }
    public string? SourceSystem { get; set; }

    public Staff? Staff { get; set; }
    public Patient? Patient { get; set; }
    public AuditEntryType? AuditEntryType { get; set; }
}
