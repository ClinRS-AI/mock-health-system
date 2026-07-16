namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyLeadership
{
    public int Id { get; set; }
    public int StudyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; }
    public int? StaffId { get; set; }

    public Study Study { get; set; } = null!;
    public Staff? Staff { get; set; }
}
