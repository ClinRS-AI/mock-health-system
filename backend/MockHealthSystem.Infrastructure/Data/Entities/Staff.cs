namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Staff
{
    public int Id { get; set; }
    public Guid? StaffUid { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
