namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyVisitArm
{
    public int VisitId { get; set; }
    public int ArmId { get; set; }

    public StudyVisit StudyVisit { get; set; } = null!;
    public StudyArm StudyArm { get; set; } = null!;
}
