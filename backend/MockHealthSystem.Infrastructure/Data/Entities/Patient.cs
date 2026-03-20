namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Patient
{
    public int Id { get; set; }
    public int? PrimarySiteId { get; set; }
    public string? DisplayName { get; set; }
    public string Status { get; set; } = "Active";
    public string? StatusReason { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? PhoneticName { get; set; }
    public string? PreferredName { get; set; }
    public string? Title { get; set; }

    public string? Country { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public bool DoNotMail { get; set; }
    public bool RecruitmentTextOptIn { get; set; }
    public string? PhoneTypeToText { get; set; }
    public string? Fax { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
    public string? GenderCode { get; set; }
    public string? Race { get; set; }
    public string? Ethnicity { get; set; }
    public string? NativeLanguage { get; set; }
    public string? MaritalStatus { get; set; }

    public double? WeightValue { get; set; }
    public string? WeightUnit { get; set; }
    public double? HeightValue { get; set; }
    public string? HeightUnit { get; set; }

    public string? Ssn { get; set; }
    public string? Mrn { get; set; }
    public long? ImportId { get; set; }
    public string? ImportSourceId { get; set; }
    public string? ImportPatientId { get; set; }
    public Guid? Uid { get; set; }

    public string? PrimaryEmailAddress { get; set; }
    public bool PrimaryDoNotEmail { get; set; }
    public string? SecondaryEmailAddress { get; set; }
    public bool SecondaryDoNotEmail { get; set; }

    public string? GuardianJson { get; set; }
    public string? PrimaryInsuranceJson { get; set; }
    public string? SecondaryInsuranceJson { get; set; }
    public string? CustomFieldsJson { get; set; }

    public bool ManagedMedicare { get; set; }
    public int? CaregiverId { get; set; }
    public bool Caregiver { get; set; }

    public Site? PrimarySite { get; set; }
    public ICollection<PatientPhone> Phones { get; set; } = new List<PatientPhone>();
    public ICollection<PatientMedicalDevice> MedicalDevices { get; set; } = new List<PatientMedicalDevice>();
    public ICollection<PatientAllergy> Allergies { get; set; } = new List<PatientAllergy>();
    public ICollection<PatientProvider> Providers { get; set; } = new List<PatientProvider>();
    public ICollection<PatientCondition> Conditions { get; set; } = new List<PatientCondition>();
    public ICollection<PatientProcedure> Procedures { get; set; } = new List<PatientProcedure>();
    public ICollection<PatientMedication> Medications { get; set; } = new List<PatientMedication>();
    public ICollection<PatientImmunization> Immunizations { get; set; } = new List<PatientImmunization>();
    public ICollection<PatientFamilyHistory> FamilyHistory { get; set; } = new List<PatientFamilyHistory>();
    public ICollection<PatientSocialHistoryEntry> SocialHistoryEntries { get; set; } = new List<PatientSocialHistoryEntry>();
}
