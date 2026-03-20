namespace MockHealthSystem.Infrastructure.Data.Entities;

public class SocialHistory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? CategoryId { get; set; }

    public ConditionType? Category { get; set; }
}
