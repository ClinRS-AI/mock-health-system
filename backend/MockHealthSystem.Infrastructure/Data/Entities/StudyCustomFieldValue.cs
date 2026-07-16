namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyCustomFieldValue
{
    public int Id { get; set; }
    public int StudyId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? FieldValue { get; set; }

    public Study Study { get; set; } = null!;
}
