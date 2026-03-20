namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Gender
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? GenderCode { get; set; }
}

