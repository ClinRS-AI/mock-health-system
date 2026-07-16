namespace MockHealthSystem.Api.Models.Studies;

public class StudyRoleViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCoordinator { get; set; }
    public bool AllowRoleSharing { get; set; }
    public bool RestrictReassignment { get; set; }
    public IList<StudyRoleStaffViewModel> Staff { get; set; } = new List<StudyRoleStaffViewModel>();
}

public class StudyRoleStaffViewModel
{
    public StaffPreviewModel Staff { get; set; } = null!;
    public string? Priority { get; set; }
}

public class StudyRoleStaffEditModel
{
    public int StaffId { get; set; }
    public string? Priority { get; set; }
}
