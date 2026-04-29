namespace MockHealthSystem.Infrastructure.Data.Entities;

/// <summary>
/// Stores named SQL queries for SOAP report execution.
/// </summary>
public class ReportQueryDefinition
{
    public int Id { get; set; }

    public string PKey { get; set; } = string.Empty;

    public string SqlQuery { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
