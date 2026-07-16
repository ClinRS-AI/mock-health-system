namespace MockHealthSystem.Api.Models.Studies;

public class StudyMilestoneViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Importance { get; set; }
    public string? Status { get; set; }
    public string? Comment { get; set; }
    public StaffPreviewModel? AssignedTo { get; set; }
    public DateTime? AssignedOn { get; set; }
    public DateTime? ProjectedDate { get; set; }
    public DateTime? CompletedOn { get; set; }
    public bool HasAutoExpenditure { get; set; }
    public MilestoneScheduleViewModel? Scheduling { get; set; }
}

public class StudyMilestoneEditModel
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Importance { get; set; }
    public string? Status { get; set; }
    public string? Comment { get; set; }
    public int? AssignedToStaffId { get; set; }
    public DateTime? AssignedOn { get; set; }
    public DateTime? ProjectedDate { get; set; }
    public DateTime? CompletedOn { get; set; }
    public bool HasAutoExpenditure { get; set; }
    public MilestoneScheduleEditModel? Scheduling { get; set; }
}

public class MilestoneScheduleViewModel
{
    public string? SchedulingMode { get; set; }
    public DateTime? DueDate { get; set; }
    public int? Offset { get; set; }
    public string? OffsetUnits { get; set; }
    public int? WindowMin { get; set; }
    public int? WindowMax { get; set; }
    public string? WindowUnits { get; set; }
}

public class MilestoneScheduleEditModel
{
    public string? SchedulingMode { get; set; }
    public DateTime? DueDate { get; set; }
    public int? Offset { get; set; }
    public string? OffsetUnits { get; set; }
    public int? WindowMin { get; set; }
    public int? WindowMax { get; set; }
    public string? WindowUnits { get; set; }
}
