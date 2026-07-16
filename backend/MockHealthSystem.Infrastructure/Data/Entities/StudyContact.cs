namespace MockHealthSystem.Infrastructure.Data.Entities;

public class StudyContact
{
    public int Id { get; set; }
    public int StudyId { get; set; }
    public string ContactType { get; set; } = string.Empty;
    public int Slot { get; set; }
    public string? Name { get; set; }
    public string? Reference { get; set; }
    public string? Comment { get; set; }

    public Study Study { get; set; } = null!;
}
