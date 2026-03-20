namespace MockHealthSystem.Infrastructure.Data.Entities;

public class PatientProcedure
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? ProcedureId { get; set; }
    public string? Name { get; set; }
    public string? Comment { get; set; }
    public string? CptCode { get; set; }
    public string? ProcedureBy { get; set; }
    public DateTime? Date { get; set; }

    public Patient Patient { get; set; } = null!;
    public Procedure? Procedure { get; set; }
}
