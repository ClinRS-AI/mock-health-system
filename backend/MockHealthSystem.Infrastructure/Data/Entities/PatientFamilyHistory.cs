namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientFamilyHistory
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int ConditionId { get; set; }
    public int FamilyMemberId { get; set; } // RelationId
    public string? RelationName { get; set; }
    public string? AgeAtOnset { get; set; }
    public string? Comment { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Patient Patient { get; set; } = null!;
    public Condition Condition { get; set; } = null!;
    public Relation Relation { get; set; } = null!;
}
