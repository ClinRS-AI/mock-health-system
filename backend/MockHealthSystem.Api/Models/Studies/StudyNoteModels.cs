namespace MockHealthSystem.Api.Models.Studies;

public class StudyNoteViewModel
{
    public int Id { get; set; }
    public StaffPreviewModel? Staff { get; set; }
    public StaffPreviewModel? LastUpdatedStaff { get; set; }
    public DateTime Date { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool Locked { get; set; }
    public bool Shared { get; set; }
}

public class StudyNoteEditModel
{
    public int? StaffId { get; set; }
    public int? LastUpdatedStaffId { get; set; }
    public DateTime? Date { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool Locked { get; set; }
    public bool Shared { get; set; }
}
