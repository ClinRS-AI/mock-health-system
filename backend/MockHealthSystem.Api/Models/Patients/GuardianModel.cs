namespace MockHealthSystem.Api.Models.Patients;

public class GuardianModel
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool AddressSameAsPatient { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? RelationshipToPatient { get; set; }
    public bool ReceivePatientPayments { get; set; }
}
