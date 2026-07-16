namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ForeColor { get; set; }
    public string? BackColor { get; set; }
}
