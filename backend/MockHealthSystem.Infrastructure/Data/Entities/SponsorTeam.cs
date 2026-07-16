namespace MockHealthSystem.Infrastructure.Data.Entities;

public class SponsorTeam
{
    public int Id { get; set; }
    public int SponsorDivisionId { get; set; }
    public string Name { get; set; } = string.Empty;

    public SponsorDivision SponsorDivision { get; set; } = null!;
}
