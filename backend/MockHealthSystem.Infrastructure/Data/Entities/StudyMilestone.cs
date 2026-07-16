namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyMilestone
{
    public int Id { get; set; }
    public int StudyId { get; set; }
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

    public string? SchedulingMode { get; set; }
    public DateTime? DueDate { get; set; }
    public int? Offset { get; set; }
    public string? OffsetUnits { get; set; }
    public int? WindowMin { get; set; }
    public int? WindowMax { get; set; }
    public string? WindowUnits { get; set; }

    public Study Study { get; set; } = null!;
    public Staff? AssignedToStaff { get; set; }
}
