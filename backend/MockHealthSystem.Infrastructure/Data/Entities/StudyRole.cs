namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyRole
{
    public int Id { get; set; }
    public int StudyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCoordinator { get; set; }
    public bool AllowRoleSharing { get; set; }
    public bool RestrictReassignment { get; set; }

    public Study Study { get; set; } = null!;
    public ICollection<StudyRoleStaff> RoleStaff { get; set; } = new List<StudyRoleStaff>();
}
