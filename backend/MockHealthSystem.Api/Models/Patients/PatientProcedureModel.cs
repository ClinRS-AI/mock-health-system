namespace MockHealthSystem.Api.Models.Patients;

public class PatientProcedureModel
{
    public int? ProcedureId { get; set; }
    public string? Comment { get; set; }
    public string? ProcedureBy { get; set; }
    public DateTime? Date { get; set; }
}
