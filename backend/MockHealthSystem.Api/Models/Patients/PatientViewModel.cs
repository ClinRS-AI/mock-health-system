namespace MockHealthSystem.Api.Models.Patients;

public class PatientViewModel
{
    public int Id { get; set; }
    public SitePreviewModel? PrimarySite { get; set; }
    public string? DisplayName { get; set; }
    public string? Status { get; set; }
    public string? StatusReason { get; set; }
    public PatientPhoneViewModel? Phone1 { get; set; }
    public PatientPhoneViewModel? Phone2 { get; set; }
    public PatientPhoneViewModel? Phone3 { get; set; }
    public PatientPhoneViewModel? Phone4 { get; set; }
    public IList<StudyPreviewModel>? ActiveStudies { get; set; }
    public IList<PatientCustomFieldModel>? CustomFields { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? PhoneticName { get; set; }
    public string? PreferredName { get; set; }
    public string? Title { get; set; }
    public PatientEmailModel? PrimaryEmail { get; set; }
    public PatientEmailModel? SecondaryEmail { get; set; }
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
    public WeightModel? Weight { get; set; }
    public HeightModel? Height { get; set; }
    public string? Ssn { get; set; }
    public string? Mrn { get; set; }
    public long? ImportId { get; set; }
    public string? ImportSourceId { get; set; }
    public string? ImportPatientId { get; set; }
    public Guid? Uid { get; set; }
    public InsuranceAccountModel? PrimaryInsurance { get; set; }
    public InsuranceAccountModel? SecondaryInsurance { get; set; }
    public bool ManagedMedicare { get; set; }
    public GuardianModel? Guardian { get; set; }
    public int? CaregiverId { get; set; }
    public bool Caregiver { get; set; }
}
