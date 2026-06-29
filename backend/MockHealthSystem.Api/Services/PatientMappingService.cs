using System.Text.Json;
using MockHealthSystem.Api.Models.Patients;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Services;

public static class PatientMappingService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static PatientViewModel ToViewModel(Patient p)
    {
        var phones = p.Phones.OrderBy(x => x.Slot).ToDictionary(x => x.Slot, x => x);
        return new PatientViewModel
        {
            Id = p.Id,
            PrimarySite = p.PrimarySite == null ? null : new SitePreviewModel { Id = p.PrimarySite.Id, Uid = p.PrimarySite.Uid, Name = p.PrimarySite.Name },
            DisplayName = p.DisplayName ?? $"{p.FirstName} {p.LastName}".Trim(),
            Status = p.Status,
            StatusReason = p.StatusReason,
            Phone1 = GetPhone(phones, 1),
            Phone2 = GetPhone(phones, 2),
            Phone3 = GetPhone(phones, 3),
            Phone4 = GetPhone(phones, 4),
            ActiveStudies = new List<StudyPreviewModel>(),
            CustomFields = Deserialize<List<PatientCustomFieldModel>>(p.CustomFieldsJson),
            FirstName = p.FirstName,
            MiddleName = p.MiddleName,
            LastName = p.LastName,
            PhoneticName = p.PhoneticName,
            PreferredName = p.PreferredName,
            Title = p.Title,
            PrimaryEmail = string.IsNullOrEmpty(p.PrimaryEmailAddress) ? null : new PatientEmailModel { Email = p.PrimaryEmailAddress, DoNotEmail = p.PrimaryDoNotEmail },
            SecondaryEmail = string.IsNullOrEmpty(p.SecondaryEmailAddress) ? null : new PatientEmailModel { Email = p.SecondaryEmailAddress, DoNotEmail = p.SecondaryDoNotEmail },
            Country = p.Country,
            Address1 = p.Address1,
            Address2 = p.Address2,
            Address3 = p.Address3,
            City = p.City,
            State = p.State,
            Zip = p.Zip,
            DoNotMail = p.DoNotMail,
            RecruitmentTextOptIn = p.RecruitmentTextOptIn,
            PhoneTypeToText = p.PhoneTypeToText,
            Fax = p.Fax,
            DateOfBirth = p.DateOfBirth,
            DateOfDeath = p.DateOfDeath,
            GenderCode = p.GenderCode,
            Race = p.Race,
            Ethnicity = p.Ethnicity,
            NativeLanguage = p.NativeLanguage,
            MaritalStatus = p.MaritalStatus,
            Weight = p.WeightValue.HasValue ? new WeightModel { Value = p.WeightValue, Unit = p.WeightUnit } : null,
            Height = p.HeightValue.HasValue ? new HeightModel { Value = p.HeightValue, Unit = p.HeightUnit } : null,
            Ssn = p.Ssn,
            Mrn = p.Mrn,
            ImportId = p.ImportId,
            ImportSourceId = p.ImportSourceId,
            ImportPatientId = p.ImportPatientId,
            Uid = p.Uid,
            PrimaryInsurance = Deserialize<InsuranceAccountModel>(p.PrimaryInsuranceJson),
            SecondaryInsurance = Deserialize<InsuranceAccountModel>(p.SecondaryInsuranceJson),
            ManagedMedicare = p.ManagedMedicare,
            Guardian = Deserialize<GuardianModel>(p.GuardianJson),
            CaregiverId = p.CaregiverId,
            Caregiver = p.Caregiver
        };
    }

    public static PatientMedicalDeviceViewModel ToViewModel(PatientMedicalDevice pd) =>
        new() { Id = pd.Id, Comment = pd.Comment, Device = pd.Device == null ? null : new DeviceViewModel { Id = pd.Device.Id, Name = pd.Device.Name } };

    public static PatientAllergiesViewModel ToAllergyViewModel(PatientAllergy pa) =>
        new() { Id = pa.Id, Reaction = pa.Reaction, Comment = pa.Comment, StartDate = pa.StartDate, EndDate = pa.EndDate, Allergy = pa.Allergy == null ? null : new PatientAllergyViewModel { Id = pa.Allergy.Id, Name = pa.Allergy.Name } };

    public static PatientProviderViewModel ToViewModel(PatientProvider pp) =>
        new() { Id = pp.Id, Comment = pp.Comment, StartDate = pp.StartDate, EndDate = pp.EndDate, Provider = pp.Provider == null ? null : new ProviderViewModel { Id = pp.Provider.Id, ProviderName = pp.Provider.ProviderName, Title = pp.Provider.Title, FirstName = pp.Provider.FirstName, MiddleName = pp.Provider.MiddleName, LastName = pp.Provider.LastName, ProviderType = pp.Provider.ProviderType == null ? null : new ProviderTypeViewModel { Id = pp.Provider.ProviderType.Id, Name = pp.Provider.ProviderType.Name } } };

    public static PatientConditionViewModel ToViewModel(PatientCondition pc) =>
        new() { Id = pc.Id, StartDate = pc.StartDate, EndDate = pc.EndDate, AgeAtOnset = pc.AgeAtOnset, Comment = pc.Comment, Condition = pc.Condition == null ? null : new ConditionPreviewViewModel { Id = pc.Condition.Id, Name = pc.Condition.Name, Icd10Code = pc.Condition.Icd10Code, Icd9Code = pc.Condition.Icd9Code } };

    public static PatientProcedureViewModel ToViewModel(PatientProcedure pp) =>
        new() { Id = pp.Id, Name = pp.Name ?? pp.Procedure?.Name, Comment = pp.Comment, CptCode = pp.CptCode ?? pp.Procedure?.CptCode, ProcedureBy = pp.ProcedureBy, Date = pp.Date };

    public static PatientMedicationViewModel ToViewModel(PatientMedication pm) =>
        new() { Id = pm.Id, Dosage = pm.Dosage, StartDate = pm.StartDate, EndDate = pm.EndDate, Comment = pm.Comment, Medication = pm.Medication == null ? null : new MedicationViewModel { Id = pm.Medication.Id, Name = pm.Medication.Name }, Route = pm.Route == null ? null : new MedicationRouteViewModel { Id = pm.Route.Id, Name = pm.Route.Name }, Conditions = pm.MedicationConditions?.Select(x => x.PatientCondition?.Condition).Where(c => c != null).Select(c => new ConditionPreviewViewModel { Id = c!.Id, Name = c.Name, Icd10Code = c.Icd10Code, Icd9Code = c.Icd9Code }).ToList() ?? [] };

    public static PatientImmunizationViewModel ToViewModel(PatientImmunization pi) =>
        new() { Id = pi.Id, Name = pi.Name ?? pi.Immunization?.Name, Comment = pi.Comment, Location = pi.Location, Date = pi.Date, ImmunizationType = pi.ImmunizationType == null ? null : new ImmunizationTypeViewModel { Id = pi.ImmunizationType.Id, Name = pi.ImmunizationType.Name } };

    public static PatientFamilyHistoryViewModel ToViewModel(PatientFamilyHistory pf) =>
        new() { Id = pf.Id, RelationName = pf.RelationName, AgeAtOnset = pf.AgeAtOnset, Comment = pf.Comment, StartDate = pf.StartDate, EndDate = pf.EndDate, Relation = pf.Relation == null ? null : new RelationViewModel { Id = pf.Relation.Id, Name = pf.Relation.Name }, Condition = pf.Condition == null ? null : new ConditionPreviewViewModel { Id = pf.Condition.Id, Name = pf.Condition.Name, Icd10Code = pf.Condition.Icd10Code, Icd9Code = pf.Condition.Icd9Code } };

    public static PatientLifeStylesHistoryViewModel ToViewModel(PatientSocialHistoryEntry pe) =>
        new() { Id = pe.Id, Comment = pe.Comment, SocialHistory = pe.SocialHistory == null ? null : new PatientSocialHistoryViewModel { Id = pe.SocialHistory.Id, Name = pe.SocialHistory.Name, Category = pe.SocialHistory.Category == null ? null : new ConditionTypeViewModel { Id = pe.SocialHistory.Category.Id, Name = pe.SocialHistory.Category.Name } } };

    private static PatientPhoneViewModel? GetPhone(IReadOnlyDictionary<int, PatientPhone> phones, int slot) =>
        phones.TryGetValue(slot, out var ph) ? new PatientPhoneViewModel { RawNumber = ph.RawNumber, Number = ph.Number, OutOfService = ph.OutOfService } : null;

    public static void ApplyEditModel(Patient entity, PatientEditModel model)
    {
        entity.PrimarySiteId = model.PrimarySiteId;
        entity.FirstName = model.FirstName;
        entity.MiddleName = model.MiddleName;
        entity.LastName = model.LastName;
        entity.PhoneticName = model.PhoneticName;
        entity.PreferredName = model.PreferredName;
        entity.Title = model.Title;
        entity.Country = model.Country;
        entity.Address1 = model.Address1;
        entity.Address2 = model.Address2;
        entity.Address3 = model.Address3;
        entity.City = model.City;
        entity.State = model.State;
        entity.Zip = model.Zip;
        entity.DoNotMail = model.DoNotMail;
        entity.RecruitmentTextOptIn = model.RecruitmentTextOptIn;
        entity.PhoneTypeToText = model.PhoneTypeToText;
        entity.Fax = model.Fax;
        entity.DateOfBirth = model.DateOfBirth;
        entity.DateOfDeath = model.DateOfDeath;
        entity.GenderCode = model.GenderCode;
        entity.Race = model.Race;
        entity.Ethnicity = model.Ethnicity;
        entity.NativeLanguage = model.NativeLanguage;
        entity.MaritalStatus = model.MaritalStatus;
        entity.WeightValue = model.Weight?.Value;
        entity.WeightUnit = model.Weight?.Unit;
        entity.HeightValue = model.Height?.Value;
        entity.HeightUnit = model.Height?.Unit;
        entity.Ssn = model.Ssn;
        entity.Mrn = model.Mrn;
        entity.ImportId = model.ImportId;
        entity.ImportSourceId = model.ImportSourceId;
        entity.ImportPatientId = model.ImportPatientId;
        entity.Uid = model.Uid;
        entity.ManagedMedicare = model.ManagedMedicare;
        entity.CaregiverId = model.CaregiverId;
        entity.Caregiver = model.Caregiver;
        entity.PrimaryEmailAddress = model.PrimaryEmail?.Email;
        entity.PrimaryDoNotEmail = model.PrimaryEmail?.DoNotEmail ?? false;
        entity.SecondaryEmailAddress = model.SecondaryEmail?.Email;
        entity.SecondaryDoNotEmail = model.SecondaryEmail?.DoNotEmail ?? false;
        entity.GuardianJson = model.Guardian == null ? null : JsonSerializer.Serialize(model.Guardian, JsonOptions);
        entity.PrimaryInsuranceJson = model.PrimaryInsurance == null ? null : JsonSerializer.Serialize(model.PrimaryInsurance, JsonOptions);
        entity.SecondaryInsuranceJson = model.SecondaryInsurance == null ? null : JsonSerializer.Serialize(model.SecondaryInsurance, JsonOptions);
        entity.CustomFieldsJson = model.CustomFields == null ? null : JsonSerializer.Serialize(model.CustomFields, JsonOptions);
    }

    public static void ApplyPatchModel(Patient entity, PatientPatchModel model)
    {
        if (model.PrimarySiteId.HasValue) entity.PrimarySiteId = model.PrimarySiteId;
        if (model.FirstName != null) entity.FirstName = model.FirstName;
        if (model.MiddleName != null) entity.MiddleName = model.MiddleName;
        if (model.LastName != null) entity.LastName = model.LastName;
        if (model.PhoneticName != null) entity.PhoneticName = model.PhoneticName;
        if (model.PreferredName != null) entity.PreferredName = model.PreferredName;
        if (model.Title != null) entity.Title = model.Title;
        if (model.Country != null) entity.Country = model.Country;
        if (model.Address1 != null) entity.Address1 = model.Address1;
        if (model.Address2 != null) entity.Address2 = model.Address2;
        if (model.Address3 != null) entity.Address3 = model.Address3;
        if (model.City != null) entity.City = model.City;
        if (model.State != null) entity.State = model.State;
        if (model.Zip != null) entity.Zip = model.Zip;
        if (model.DoNotMail.HasValue) entity.DoNotMail = model.DoNotMail.Value;
        if (model.RecruitmentTextOptIn.HasValue) entity.RecruitmentTextOptIn = model.RecruitmentTextOptIn.Value;
        if (model.PhoneTypeToText != null) entity.PhoneTypeToText = model.PhoneTypeToText;
        if (model.Fax != null) entity.Fax = model.Fax;
        if (model.DateOfBirth.HasValue) entity.DateOfBirth = model.DateOfBirth;
        if (model.DateOfDeath.HasValue) entity.DateOfDeath = model.DateOfDeath;
        if (model.GenderCode != null) entity.GenderCode = model.GenderCode;
        if (model.Race != null) entity.Race = model.Race;
        if (model.Ethnicity != null) entity.Ethnicity = model.Ethnicity;
        if (model.NativeLanguage != null) entity.NativeLanguage = model.NativeLanguage;
        if (model.MaritalStatus != null) entity.MaritalStatus = model.MaritalStatus;
        if (model.Weight != null) { entity.WeightValue = model.Weight.Value; entity.WeightUnit = model.Weight.Unit; }
        if (model.Height != null) { entity.HeightValue = model.Height.Value; entity.HeightUnit = model.Height.Unit; }
        if (model.Ssn != null) entity.Ssn = model.Ssn;
        if (model.Mrn != null) entity.Mrn = model.Mrn;
        if (model.ImportId.HasValue) entity.ImportId = model.ImportId;
        if (model.ImportSourceId != null) entity.ImportSourceId = model.ImportSourceId;
        if (model.ImportPatientId != null) entity.ImportPatientId = model.ImportPatientId;
        if (model.Uid.HasValue) entity.Uid = model.Uid;
        if (model.ManagedMedicare.HasValue) entity.ManagedMedicare = model.ManagedMedicare.Value;
        if (model.CaregiverId.HasValue) entity.CaregiverId = model.CaregiverId;
        if (model.Caregiver.HasValue) entity.Caregiver = model.Caregiver.Value;
        if (model.PrimaryEmail != null) { entity.PrimaryEmailAddress = model.PrimaryEmail.Email; entity.PrimaryDoNotEmail = model.PrimaryEmail.DoNotEmail; }
        if (model.SecondaryEmail != null) { entity.SecondaryEmailAddress = model.SecondaryEmail.Email; entity.SecondaryDoNotEmail = model.SecondaryEmail.DoNotEmail; }
        if (model.Guardian != null) entity.GuardianJson = JsonSerializer.Serialize(model.Guardian, JsonOptions);
        if (model.PrimaryInsurance != null) entity.PrimaryInsuranceJson = JsonSerializer.Serialize(model.PrimaryInsurance, JsonOptions);
        if (model.SecondaryInsurance != null) entity.SecondaryInsuranceJson = JsonSerializer.Serialize(model.SecondaryInsurance, JsonOptions);
        if (model.CustomFields != null) entity.CustomFieldsJson = JsonSerializer.Serialize(model.CustomFields, JsonOptions);
        ApplyPhoneSlot(entity, 1, model.Phone1);
        ApplyPhoneSlot(entity, 2, model.Phone2);
        ApplyPhoneSlot(entity, 3, model.Phone3);
        ApplyPhoneSlot(entity, 4, model.Phone4);
    }

    // Upserts in place rather than replacing, so an omitted slot in a PATCH body stays untouched.
    // PUT (PatientsController.SyncPhonesFromEdit) deletes and recreates instead, since a full
    // replace must clear slots the caller didn't send. Don't unify these without changing one
    // of those semantics.
    private static void ApplyPhoneSlot(Patient entity, int slot, PatientPhoneEditModel? model)
    {
        if (model == null) return;
        var rawNumber = string.IsNullOrEmpty(model.Number) ? null : new string(model.Number.Where(char.IsDigit).ToArray());
        var phone = entity.Phones.FirstOrDefault(p => p.Slot == slot);
        if (phone == null)
        {
            entity.Phones.Add(new PatientPhone { PatientId = entity.Id, Slot = slot, Number = model.Number, RawNumber = rawNumber });
        }
        else
        {
            phone.Number = model.Number;
            phone.RawNumber = rawNumber;
        }
    }

    public static void ApplyStatus(Patient entity, PatientStatusEditModel model)
    {
        entity.Status = model.Status;
        entity.StatusReason = model.Reason;
    }

    private static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json, JsonOptions); } catch { return null; }
    }
}
