namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyTargetDate
{
    public int Id { get; set; }
    public int StudyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Tooltip { get; set; }
    public bool Required { get; set; }
    public DateTime? TargetDate { get; set; }

    public Study Study { get; set; } = null!;
}
