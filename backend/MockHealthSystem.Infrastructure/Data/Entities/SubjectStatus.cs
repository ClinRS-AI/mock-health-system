namespace MockHealthSystem.Infrastructure.Data.Entities;

public class SubjectStatus
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime ChangedOn { get; set; }
    public int? ChangedByStaffId { get; set; }
    public string? Comment { get; set; }

    public Subject Subject { get; set; } = null!;
    public Staff? ChangedByStaff { get; set; }
}
