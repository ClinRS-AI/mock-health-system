namespace MockHealthSystem.Api.Models.System;

using MockHealthSystem.Infrastructure.Data.Entities;

public class SysConditionViewModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Icd10Code { get; set; }

    public string? Icd9Code { get; set; }

    public string? Description { get; set; }

    public string? GenderCode { get; set; }

    public bool ChildBearing { get; set; }

    public SysConditionTypeViewModel? Category { get; set; }

    public static SysConditionViewModel FromEntity(Condition entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Icd10Code = entity.Icd10Code,
            Icd9Code = entity.Icd9Code,
            Description = entity.Description,
            GenderCode = entity.GenderCode,
            ChildBearing = entity.ChildBearing,
            Category = entity.ConditionType is null
                ? null
                : new SysConditionTypeViewModel
                {
                    Id = entity.ConditionType.Id,
                    Name = entity.ConditionType.Name,
                    Description = entity.ConditionType.Description
                }
        };
}

public class SysConditionTypeViewModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}


