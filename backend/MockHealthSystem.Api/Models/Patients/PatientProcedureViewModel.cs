namespace MockHealthSystem.Api.Models.Patients;

public class PatientProcedureViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Comment { get; set; }
    public string? CptCode { get; set; }
    public string? ProcedureBy { get; set; }
    public DateTime? Date { get; set; }
}
