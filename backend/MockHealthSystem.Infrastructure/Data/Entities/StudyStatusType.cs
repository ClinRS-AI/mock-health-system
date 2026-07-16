namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyStatusType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BackColor { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEnrollmentPermitted { get; set; }
    public string? StudyPhase { get; set; }
}
