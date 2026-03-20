namespace MockHealthSystem.Api.Models.Patients;

public class PatientSearchCriteria
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Zip { get; set; }
    public string? City { get; set; }
    public string? Status { get; set; }

    /// <summary>Max number of records to return. Default 100, max 5000.</summary>
    public int? Limit { get; set; }

    /// <summary>Number of records to skip (for pagination). Default 0.</summary>
    public int? Skip { get; set; }
}
