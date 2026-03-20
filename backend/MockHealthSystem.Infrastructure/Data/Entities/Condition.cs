namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Condition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icd10Code { get; set; }
    public string? Icd9Code { get; set; }

    public string? Description { get; set; }

    public string? GenderCode { get; set; }

    public bool ChildBearing { get; set; }

    public int? ConditionTypeId { get; set; }

    public ConditionType? ConditionType { get; set; }
}
