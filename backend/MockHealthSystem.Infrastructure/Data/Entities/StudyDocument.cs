namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyDocument
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int StudyId { get; set; }
    public string? TypeName { get; set; }
    public string? TypeCategory { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Source { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public Study Study { get; set; } = null!;
    public ICollection<StudyDocumentStatusHistory> StatusHistory { get; set; } = new List<StudyDocumentStatusHistory>();
}
