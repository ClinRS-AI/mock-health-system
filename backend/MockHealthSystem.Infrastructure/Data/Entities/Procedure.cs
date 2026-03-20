namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Procedure
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CptCode { get; set; }
}
