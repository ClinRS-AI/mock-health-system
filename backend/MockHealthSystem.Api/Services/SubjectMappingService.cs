using MockHealthSystem.Api.Models.Patients;
using MockHealthSystem.Api.Models.Subjects;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Services;

public static class SubjectMappingService
{
    /// <summary>Normalize to UTC so Npgsql accepts it for timestamp with time zone (rejects
    /// Unspecified/Local). Mirrors StudyMappingService.ToUtc / PatientsController.ToUtc.</summary>
    private static DateTime? ToUtc(DateTime? value)
    {
        if (value == null) return null;
        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            _ => value.Value.ToUniversalTime()
        };
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value.ToUniversalTime()
    };

    public static SubjectViewModel ToViewModel(Subject s) => new()
    {
        Id = s.Id,
        Uid = s.Uid,
        Study = StudyMappingService.ToPreview(s.Study),
        Site = ToPreview(s.Site),
        Patient = ToPatientPreview(s.Patient),
        Status = s.Status,
        ProtocolVersion = s.ProtocolVersion == null ? null : StudyMappingService.ToPreview(s.ProtocolVersion),
        GenderCode = s.GenderCode,
        Race = s.Race,
        Ethnicity = s.Ethnicity,
        Arm = s.StudyArm == null ? null : StudyMappingService.ToPreview(s.StudyArm),
        ImportId = s.ImportId,
        Tag = s.Tag,
        FacilityCode = s.FacilityCode,
        EnrollmentDate = s.EnrollmentDate,
        EnrollmentLocation = s.EnrollmentLocation,
        ScreeningNumber = s.ScreeningNumber,
        RandomizationNumber = s.RandomizationNumber,
        TreatmentStatus = s.TreatmentStatus,
        TreatmentStart = s.TreatmentStart,
        Narrative = s.Narrative,
        CreatedOn = s.CreatedOn,
        LastUpdatedOn = s.LastUpdatedOn
    };

    private static SitePreviewModel? ToPreview(Site? site) => site == null
        ? null
        : new SitePreviewModel { Id = site.Id, Uid = site.Uid, Name = site.Name };

    private static SubjectPatientPreviewModel ToPatientPreview(Patient p) => new()
    {
        Id = p.Id,
        Uid = p.Uid,
        FirstName = p.FirstName,
        MiddleName = p.MiddleName,
        LastName = p.LastName,
        Title = p.Title,
        GenderCode = p.GenderCode,
        Race = p.Race,
        Ethnicity = p.Ethnicity,
        DateOfBirth = p.DateOfBirth,
        Name = $"{p.LastName}, {p.FirstName}"
    };

    /// <summary>Applies POST-only fields (research.md Decision 13). Deliberately does not touch
    /// StudyArmId/ProtocolVersionId/GenderCode/Race/Ethnicity — those aren't part of
    /// SubjectCreateModel and stay at the entity's defaults (null) until set via PUT/PATCH. Status
    /// is always SubjectStatusCatalog.InitialStatus; CC's create body doesn't accept a status.</summary>
    public static void ApplyCreateModel(Subject entity, SubjectCreateModel model)
    {
        entity.PatientId = model.PatientId;
        entity.StudyId = model.StudyId;
        entity.SiteId = model.SiteId;
        entity.Status = SubjectStatusCatalog.InitialStatus;
        entity.ImportId = model.ImportId;
        entity.Tag = model.Tag;
        entity.FacilityCode = model.FacilityCode;
        entity.EnrollmentDate = ToUtc(model.EnrollmentDate);
        entity.EnrollmentLocation = model.EnrollmentLocation;
        entity.ScreeningNumber = model.ScreeningNumber;
        entity.RandomizationNumber = model.RandomizationNumber;
        entity.TreatmentStatus = model.TreatmentStatus;
        entity.TreatmentStart = ToUtc(model.TreatmentStart);
        entity.Narrative = model.Narrative;
        entity.LastUpdatedOn = DateTime.UtcNow;
    }

    public static void ApplyEditModel(Subject entity, SubjectEditModel model)
    {
        entity.PatientId = model.PatientId;
        entity.StudyId = model.StudyId;
        entity.SiteId = model.SiteId;
        entity.StudyArmId = model.StudyArmId;
        entity.ProtocolVersionId = model.ProtocolVersionId;
        entity.Status = model.Status;
        entity.GenderCode = model.GenderCode;
        entity.Race = model.Race;
        entity.Ethnicity = model.Ethnicity;
        entity.ImportId = model.ImportId;
        entity.Tag = model.Tag;
        entity.FacilityCode = model.FacilityCode;
        entity.EnrollmentDate = ToUtc(model.EnrollmentDate);
        entity.EnrollmentLocation = model.EnrollmentLocation;
        entity.ScreeningNumber = model.ScreeningNumber;
        entity.RandomizationNumber = model.RandomizationNumber;
        entity.TreatmentStatus = model.TreatmentStatus;
        entity.TreatmentStart = ToUtc(model.TreatmentStart);
        entity.Narrative = model.Narrative;
        entity.LastUpdatedOn = DateTime.UtcNow;
    }

    public static void ApplyPatchModel(Subject entity, SubjectPatchModel model)
    {
        if (model.PatientId.HasValue) entity.PatientId = model.PatientId.Value;
        if (model.StudyId.HasValue) entity.StudyId = model.StudyId.Value;
        if (model.SiteId.HasValue) entity.SiteId = model.SiteId;
        if (model.StudyArmId.HasValue) entity.StudyArmId = model.StudyArmId;
        if (model.ProtocolVersionId.HasValue) entity.ProtocolVersionId = model.ProtocolVersionId;
        if (model.Status != null) entity.Status = model.Status;
        if (model.GenderCode != null) entity.GenderCode = model.GenderCode;
        if (model.Race != null) entity.Race = model.Race;
        if (model.Ethnicity != null) entity.Ethnicity = model.Ethnicity;
        if (model.ImportId != null) entity.ImportId = model.ImportId;
        if (model.Tag != null) entity.Tag = model.Tag;
        if (model.FacilityCode != null) entity.FacilityCode = model.FacilityCode;
        if (model.EnrollmentDate.HasValue) entity.EnrollmentDate = ToUtc(model.EnrollmentDate.Value);
        if (model.EnrollmentLocation != null) entity.EnrollmentLocation = model.EnrollmentLocation;
        if (model.ScreeningNumber != null) entity.ScreeningNumber = model.ScreeningNumber;
        if (model.RandomizationNumber != null) entity.RandomizationNumber = model.RandomizationNumber;
        if (model.TreatmentStatus != null) entity.TreatmentStatus = model.TreatmentStatus;
        if (model.TreatmentStart.HasValue) entity.TreatmentStart = ToUtc(model.TreatmentStart);
        if (model.Narrative != null) entity.Narrative = model.Narrative;
        entity.LastUpdatedOn = DateTime.UtcNow;
    }

    public static SubjectStatusViewModel ToViewModel(SubjectStatus h) => new()
    {
        Id = h.Id,
        SubjectId = h.SubjectId,
        StatusName = h.StatusName,
        ChangedOn = h.ChangedOn,
        ChangedBy = StudyMappingService.ToStaffPreview(h.ChangedByStaff),
        Comment = h.Comment
    };
}
