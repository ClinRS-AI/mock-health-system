using MockHealthSystem.Api.Models;

namespace MockHealthSystem.Api.Models.Patients;

/// <summary>
/// Partial update model for patient (PATCH). Every field is <see cref="Optional{T}"/> so a field
/// that is omitted from the request body is left untouched, while a field explicitly sent as
/// null clears it.
/// </summary>
/// <remarks>
/// This fix is scoped to Patient only. <c>StudyPatchModel</c> and <c>SubjectPatchModel</c> still
/// use plain nullable fields and share the same "can't clear a field to null" limitation this
/// type solves — that's a known gap, not an oversight, and left for a separate change.
/// </remarks>
public class PatientPatchModel
{
    public Optional<int?> PrimarySiteId { get; set; }
    public Optional<PatientPhoneEditModel?> Phone1 { get; set; }
    public Optional<PatientPhoneEditModel?> Phone2 { get; set; }
    public Optional<PatientPhoneEditModel?> Phone3 { get; set; }
    public Optional<PatientPhoneEditModel?> Phone4 { get; set; }
    public Optional<IList<PatientCustomFieldModel>?> CustomFields { get; set; }
    public Optional<string?> FirstName { get; set; }
    public Optional<string?> MiddleName { get; set; }
    public Optional<string?> LastName { get; set; }
    public Optional<string?> PhoneticName { get; set; }
    public Optional<string?> PreferredName { get; set; }
    public Optional<string?> Title { get; set; }
    public Optional<PatientEmailModel?> PrimaryEmail { get; set; }
    public Optional<PatientEmailModel?> SecondaryEmail { get; set; }
    public Optional<string?> Country { get; set; }
    public Optional<string?> Address1 { get; set; }
    public Optional<string?> Address2 { get; set; }
    public Optional<string?> Address3 { get; set; }
    public Optional<string?> City { get; set; }
    public Optional<string?> State { get; set; }
    public Optional<string?> Zip { get; set; }
    public Optional<bool> DoNotMail { get; set; }
    public Optional<bool> RecruitmentTextOptIn { get; set; }
    public Optional<string?> PhoneTypeToText { get; set; }
    public Optional<string?> Fax { get; set; }
    public Optional<DateTime?> DateOfBirth { get; set; }
    public Optional<DateTime?> DateOfDeath { get; set; }
    public Optional<string?> GenderCode { get; set; }
    public Optional<string?> Race { get; set; }
    public Optional<string?> Ethnicity { get; set; }
    public Optional<string?> NativeLanguage { get; set; }
    public Optional<string?> MaritalStatus { get; set; }
    public Optional<WeightModel?> Weight { get; set; }
    public Optional<HeightModel?> Height { get; set; }
    public Optional<string?> Ssn { get; set; }
    public Optional<string?> Mrn { get; set; }
    public Optional<long?> ImportId { get; set; }
    public Optional<string?> ImportSourceId { get; set; }
    public Optional<string?> ImportPatientId { get; set; }
    public Optional<Guid?> Uid { get; set; }
    public Optional<InsuranceAccountModel?> PrimaryInsurance { get; set; }
    public Optional<InsuranceAccountModel?> SecondaryInsurance { get; set; }
    public Optional<bool> ManagedMedicare { get; set; }
    public Optional<GuardianModel?> Guardian { get; set; }
    public Optional<int?> CaregiverId { get; set; }
    public Optional<bool> Caregiver { get; set; }
}
