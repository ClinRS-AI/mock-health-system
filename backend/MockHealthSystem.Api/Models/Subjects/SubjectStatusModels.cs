using MockHealthSystem.Api.Models.Studies;

namespace MockHealthSystem.Api.Models.Subjects;

public class SubjectStatusViewModel
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime ChangedOn { get; set; }
    public StaffPreviewModel? ChangedBy { get; set; }
    public string? Comment { get; set; }
}
