namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Allergy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? AllergenTypeId { get; set; }

    public AllergenType? AllergenType { get; set; }
}
