namespace MockHealthSystem.Api.Models.Patients;

public class PatientSocialHistoryViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public ConditionTypeViewModel? Category { get; set; }
}
