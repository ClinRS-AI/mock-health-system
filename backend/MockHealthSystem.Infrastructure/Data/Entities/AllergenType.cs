namespace MockHealthSystem.Infrastructure.Data.Entities;

public class AllergenType
{
    public int Id { get; set; }

    public string AllergenTypeId { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsDefault { get; set; }
}

