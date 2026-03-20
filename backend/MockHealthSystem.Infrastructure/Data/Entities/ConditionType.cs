namespace MockHealthSystem.Infrastructure.Data.Entities;

public class ConditionType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
