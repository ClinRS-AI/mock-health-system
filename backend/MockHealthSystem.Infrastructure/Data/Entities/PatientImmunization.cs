namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientImmunization
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int ImmunizationId { get; set; }
    public int? ImmunizationTypeId { get; set; }
    public string? Name { get; set; }
    public string? Comment { get; set; }
    public string? Location { get; set; }
    public DateTime? Date { get; set; }

    public Patient Patient { get; set; } = null!;
    public Immunization Immunization { get; set; } = null!;
    public ImmunizationType? ImmunizationType { get; set; }
}
