namespace MockHealthSystem.Api.Models.Patients;

public class ConditionPreviewViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Icd10Code { get; set; }
    public string? Icd9Code { get; set; }
}
