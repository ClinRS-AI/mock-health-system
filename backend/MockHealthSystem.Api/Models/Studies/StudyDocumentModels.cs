namespace MockHealthSystem.Api.Models.Studies;

public class StudyDocumentViewModel
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string? TypeName { get; set; }
    public string? TypeCategory { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Source { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

public class StudyDocumentEditModel
{
    public string? TypeName { get; set; }
    public string? TypeCategory { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Source { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

public class StudyDocumentStatusHistoryViewModel
{
    public int Id { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime ChangedOn { get; set; }
    public StaffPreviewModel? ChangedBy { get; set; }
    public string? Comment { get; set; }
}
