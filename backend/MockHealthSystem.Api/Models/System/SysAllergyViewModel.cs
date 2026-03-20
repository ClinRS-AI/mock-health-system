namespace MockHealthSystem.Api.Models.System;

using MockHealthSystem.Infrastructure.Data.Entities;

public class SysAllergyViewModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public SysAllergenTypeViewModel? Allergen { get; set; }

    public static SysAllergyViewModel FromEntity(Allergy entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Allergen = entity.AllergenType is null
                ? null
                : new SysAllergenTypeViewModel
                {
                    Id = entity.AllergenType.Id,
                    AllergenTypeId = entity.AllergenType.AllergenTypeId,
                    Description = entity.AllergenType.Description,
                    IsDefault = entity.AllergenType.IsDefault
                }
        };
}

public class SysAllergenTypeViewModel
{
    public int Id { get; set; }

    public string? AllergenTypeId { get; set; }

    public string? Description { get; set; }

    public bool IsDefault { get; set; }
}

