namespace MockHealthSystem.Api.Models.System;

using MockHealthSystem.Infrastructure.Data.Entities;

public class SysMedicationViewModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public bool ChildBearing { get; set; }

    public SysMedicationTypeViewModel? Category { get; set; }

    public SysGenderViewModel? Gender { get; set; }

    public SysMedicationRouteViewModel? DefaultRoute { get; set; }

    public SysMedicationScheduleViewModel? DefaultSchedule { get; set; }

    public static SysMedicationViewModel FromEntity(Medication entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ChildBearing = entity.ChildBearing,
            Category = entity.MedicationType is null
                ? null
                : new SysMedicationTypeViewModel
                {
                    Id = entity.MedicationType.Id,
                    Name = entity.MedicationType.Name,
                    Description = entity.MedicationType.Description
                },
            Gender = entity.Gender is null
                ? null
                : new SysGenderViewModel
                {
                    Id = entity.Gender.Id,
                    Name = entity.Gender.Name,
                    GenderCode = entity.Gender.GenderCode
                },
            DefaultRoute = entity.DefaultRoute is null
                ? null
                : new SysMedicationRouteViewModel
                {
                    Id = entity.DefaultRoute.Id,
                    Name = entity.DefaultRoute.Name,
                    Description = null
                },
            DefaultSchedule = entity.DefaultSchedule is null
                ? null
                : new SysMedicationScheduleViewModel
                {
                    Id = entity.DefaultSchedule.Id,
                    Name = entity.DefaultSchedule.Name,
                    Description = entity.DefaultSchedule.Description
                }
        };
}

public class SysMedicationTypeViewModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}

public class SysGenderViewModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? GenderCode { get; set; }
}

public class SysMedicationRouteViewModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}

public class SysMedicationScheduleViewModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}


