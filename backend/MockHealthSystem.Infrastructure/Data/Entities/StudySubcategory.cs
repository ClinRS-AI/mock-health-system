namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudySubcategory
{
    public int Id { get; set; }
    public int? StudyCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public StudyCategory? StudyCategory { get; set; }
}
