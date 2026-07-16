namespace MockHealthSystem.Infrastructure.Data.Entities;

public class SponsorDivision
{
    public int Id { get; set; }
    public int SponsorId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Sponsor Sponsor { get; set; } = null!;
}
