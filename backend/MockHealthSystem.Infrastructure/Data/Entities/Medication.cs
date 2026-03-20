namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Medication
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool ChildBearing { get; set; }

    public int? MedicationTypeId { get; set; }

    public MedicationType? MedicationType { get; set; }

    public int? GenderId { get; set; }

    public Gender? Gender { get; set; }

    public int? DefaultRouteId { get; set; }

    public MedicationRoute? DefaultRoute { get; set; }

    public int? DefaultScheduleId { get; set; }

    public MedicationSchedule? DefaultSchedule { get; set; }
}
