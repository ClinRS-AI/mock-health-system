namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Site
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
}
