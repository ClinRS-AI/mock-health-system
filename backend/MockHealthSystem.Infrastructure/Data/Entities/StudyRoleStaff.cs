namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyRoleStaff
{
    public int StudyRoleId { get; set; }
    public int StaffId { get; set; }
    public string? Priority { get; set; }

    public StudyRole StudyRole { get; set; } = null!;
    public Staff Staff { get; set; } = null!;
}
