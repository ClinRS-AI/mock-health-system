namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyNote
{
    public int Id { get; set; }
    public int StudyId { get; set; }
    public int? StaffId { get; set; }
    public int? LastUpdatedStaffId { get; set; }
    public DateTime NoteDate { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool Locked { get; set; }
    public bool Shared { get; set; }

    public Study Study { get; set; } = null!;
    public Staff? Staff { get; set; }
    public Staff? LastUpdatedStaff { get; set; }
}
