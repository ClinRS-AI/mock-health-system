namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyStudyType
{
    public int StudyId { get; set; }
    public int StudyTypeId { get; set; }

    public Study Study { get; set; } = null!;
    public StudyType StudyType { get; set; } = null!;
}
