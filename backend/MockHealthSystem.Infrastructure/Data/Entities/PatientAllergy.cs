namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientAllergy
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int AllergyId { get; set; }
    public string? Reaction { get; set; }
    public string? Comment { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Patient Patient { get; set; } = null!;
    public Allergy Allergy { get; set; } = null!;
}
