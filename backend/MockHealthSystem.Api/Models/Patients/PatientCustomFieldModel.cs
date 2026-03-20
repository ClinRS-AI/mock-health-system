namespace MockHealthSystem.Api.Models.Patients;

public class PatientCustomFieldModel
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public DateTime? ValueDate { get; set; }
}
