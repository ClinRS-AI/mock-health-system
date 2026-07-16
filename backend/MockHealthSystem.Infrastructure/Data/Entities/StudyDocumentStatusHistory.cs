namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyDocumentStatusHistory
{
    public int Id { get; set; }
    public int StudyDocumentId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime ChangedOn { get; set; }
    public int? ChangedByStaffId { get; set; }
    public string? Comment { get; set; }

    public StudyDocument StudyDocument { get; set; } = null!;
    public Staff? ChangedByStaff { get; set; }
}
