using MockHealthSystem.Api.Models.Patients;
using MockHealthSystem.Api.Models.Studies;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Services;

public static class StudyMappingService
{
    /// <summary>Normalize to UTC so Npgsql accepts it for timestamp with time zone (rejects
    /// Unspecified/Local). Mirrors PatientsController.ToUtc — client-supplied dates deserialize
    /// as Unspecified/Local unless the JSON carries an explicit UTC offset.</summary>
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

    // ---- Study core ----

    public static StudyViewModel ToViewModel(Study s) => new()
    {
        Id = s.Id,
        Uid = s.Uid,
        Name = s.Name,
        Title = s.Title,
        Identifier = s.Identifier,
        ProtocolNumber = s.ProtocolNumber,
        IndIdeNumber = s.IndIdeNumber,
        NctNumber = s.NctNumber,
        Phase = s.Phase,
        Status = s.Status,
        Category = s.Category,
        Subcategory = s.Subcategory,
        StudyGroup = s.StudyGroup,
        Tag1 = s.Tag1,
        Tag2 = s.Tag2,
        Tag3 = s.Tag3,
        Tag4 = s.Tag4,
        Comment = s.Comment,
        Description = s.Description,
        LaunchYear = s.LaunchYear,
        StudyCurrency = s.StudyCurrency,
        SponsorTeam = new SponsorTeamPreviewModel { Id = s.SponsorTeam.Id, Name = s.SponsorTeam.Name },
        ManagingSite = s.ManagingSite == null ? null : new SitePreviewModel { Id = s.ManagingSite.Id, Uid = s.ManagingSite.Uid, Name = s.ManagingSite.Name },
        Finances = new StudyFinanceModel
        {
            FinanceType = s.FinanceType,
            AccountingCode1 = s.AccountingCode1,
            AccountingCode2 = s.AccountingCode2,
            AccountingCode3 = s.AccountingCode3,
            AccountingCode4 = s.AccountingCode4
        },
        OpportunityDetails = new StudyOpportunityModel
        {
            OpportunityLevel = s.OpportunityLevel,
            Probability = s.OpportunityProbability,
            ExpectedDate = s.OpportunityExpectedDate,
            ExpectedNumberOfSites = s.OpportunityExpectedNumberOfSites,
            Comment = s.OpportunityComment
        },
        EnrollmentNote = s.EnrollmentNote,
        BudgetNote = s.BudgetNote,
        RegulatoryNote = s.RegulatoryNote,
        ContractNote = s.ContractNote,
        StudyLead = new StudyLeadSourceViewModel
        {
            Staff = ToStaffPreview(s.LeadSourceStaff),
            Source = s.LeadSource,
            Date = s.LeadDate,
            Comment = s.LeadComment
        },
        TargetDates = s.TargetDates
            .Select(t => new StudyTargetDateViewModel { Id = t.Id, Name = t.Name, Tooltip = t.Tooltip, Required = t.Required, Date = t.TargetDate })
            .ToList(),
        Leadership = s.Leadership
            .Select(l => new StudyLeaderViewModel { Id = l.Id, Name = l.Name, Required = l.Required, Staff = ToStaffPreview(l.Staff) })
            .ToList(),
        CustomFields = s.CustomFieldValues
            .Select(c => new StudyCustomFieldModel { FieldName = c.FieldName, FieldValue = c.FieldValue })
            .ToList(),
        Contacts = s.Contacts
            .Select(c => new StudyContactEntryViewModel { Type = c.ContactType, Slot = c.Slot, Name = c.Name, Reference = c.Reference, Comment = c.Comment })
            .ToList(),
        CreatedOn = s.CreatedOn,
        LastUpdatedOn = s.LastUpdatedOn
    };

    public static StudyPreviewModel ToPreview(Study s) => new() { Id = s.Id, Uid = s.Uid, Name = s.Name };

    /// <summary>Sets flat scalar fields only. Embedded collections (target dates, leadership,
    /// custom fields, contacts) are synced separately via the Sync*FromEdit methods below —
    /// mirrors how PatientMappingService.ApplyEditModel leaves Phones to the caller.</summary>
    public static void ApplyEditModel(Study entity, StudyEditModel model)
    {
        entity.SponsorTeamId = model.SponsorTeamId;
        entity.ManagingSiteId = model.ManagingSiteId;
        entity.Name = model.Name;
        entity.Title = model.Title;
        entity.Identifier = model.Identifier;
        entity.ProtocolNumber = model.ProtocolNumber;
        entity.IndIdeNumber = model.IndIdeNumber;
        entity.NctNumber = model.NctNumber;
        entity.Phase = model.Phase;
        entity.Status = model.Status;
        entity.Category = model.Category;
        entity.Subcategory = model.Subcategory;
        entity.StudyGroup = model.StudyGroup;
        entity.Tag1 = model.Tag1;
        entity.Tag2 = model.Tag2;
        entity.Tag3 = model.Tag3;
        entity.Tag4 = model.Tag4;
        entity.Comment = model.Comment;
        entity.Description = model.Description;
        entity.LaunchYear = model.LaunchYear;
        entity.StudyCurrency = model.StudyCurrency;
        entity.FinanceType = model.Finances?.FinanceType;
        entity.AccountingCode1 = model.Finances?.AccountingCode1;
        entity.AccountingCode2 = model.Finances?.AccountingCode2;
        entity.AccountingCode3 = model.Finances?.AccountingCode3;
        entity.AccountingCode4 = model.Finances?.AccountingCode4;
        entity.OpportunityLevel = model.OpportunityDetails?.OpportunityLevel;
        entity.OpportunityProbability = model.OpportunityDetails?.Probability;
        entity.OpportunityExpectedDate = ToUtc(model.OpportunityDetails?.ExpectedDate);
        entity.OpportunityExpectedNumberOfSites = model.OpportunityDetails?.ExpectedNumberOfSites;
        entity.OpportunityComment = model.OpportunityDetails?.Comment;
        entity.EnrollmentNote = model.EnrollmentNote;
        entity.BudgetNote = model.BudgetNote;
        entity.RegulatoryNote = model.RegulatoryNote;
        entity.ContractNote = model.ContractNote;
        entity.LeadSourceStaffId = model.StudyLead?.StaffId;
        entity.LeadSource = model.StudyLead?.Source;
        entity.LeadDate = ToUtc(model.StudyLead?.Date);
        entity.LeadComment = model.StudyLead?.Comment;
        entity.Uid = model.Uid ?? entity.Uid;
    }

    /// <summary>Add-only: appends rows from the edit model's embedded arrays. Used for POST
    /// (entity starts with empty collections) and PUT (caller clears existing rows first —
    /// see StudiesController.SyncEmbeddedCollectionsFromEdit).</summary>
    public static void SyncTargetDatesFromEdit(Study entity, StudyEditModel model)
    {
        if (model.TargetDates == null) return;
        foreach (var t in model.TargetDates)
            entity.TargetDates.Add(new StudyTargetDate { Name = t.Name, Tooltip = t.Tooltip, Required = t.Required, TargetDate = ToUtc(t.Date) });
    }

    public static void SyncLeadershipFromEdit(Study entity, StudyEditModel model)
    {
        if (model.Leadership == null) return;
        foreach (var l in model.Leadership)
            entity.Leadership.Add(new StudyLeadership { Name = l.Name, Required = l.Required, StaffId = l.StaffId });
    }

    public static void SyncCustomFieldsFromEdit(Study entity, StudyEditModel model)
    {
        if (model.CustomFields == null) return;
        foreach (var c in model.CustomFields)
            entity.CustomFieldValues.Add(new StudyCustomFieldValue { FieldName = c.FieldName, FieldValue = c.FieldValue });
    }

    /// <summary>Appends unconditionally with no duplicate guard — callers must call
    /// ValidateContacts first, or a duplicate (Type, Slot) pair will hit the unique index as an
    /// unhandled DbUpdateException instead of a clean 400.</summary>
    public static void SyncContactsFromEdit(Study entity, StudyEditModel model)
    {
        if (model.Contacts == null) return;
        foreach (var c in model.Contacts)
            entity.Contacts.Add(new StudyContact { ContactType = c.Type, Slot = c.Slot, Name = c.Name, Reference = c.Reference, Comment = c.Comment });
    }

    /// <summary>Rejects duplicate (Type, Slot) pairs before SyncContactsFromEdit, which appends
    /// unconditionally and would otherwise hit the unique index and throw a raw 500.</summary>
    public static string? ValidateContacts(IEnumerable<StudyContactEntryEditModel>? contacts)
    {
        if (contacts == null) return null;
        var seen = new HashSet<(string Type, int Slot)>();
        foreach (var c in contacts)
        {
            if (!seen.Add((c.Type, c.Slot)))
                return $"Duplicate contact entry for type '{c.Type}' slot {c.Slot}.";
        }
        return null;
    }

    public static void ApplyPatchModel(Study entity, StudyPatchModel model)
    {
        if (model.SponsorTeamId.HasValue) entity.SponsorTeamId = model.SponsorTeamId.Value;
        if (model.ManagingSiteId.HasValue) entity.ManagingSiteId = model.ManagingSiteId;
        if (model.Name != null) entity.Name = model.Name;
        if (model.Title != null) entity.Title = model.Title;
        if (model.Identifier != null) entity.Identifier = model.Identifier;
        if (model.ProtocolNumber != null) entity.ProtocolNumber = model.ProtocolNumber;
        if (model.IndIdeNumber != null) entity.IndIdeNumber = model.IndIdeNumber;
        if (model.NctNumber != null) entity.NctNumber = model.NctNumber;
        if (model.Phase != null) entity.Phase = model.Phase;
        if (model.Status != null) entity.Status = model.Status;
        if (model.Category != null) entity.Category = model.Category;
        if (model.Subcategory != null) entity.Subcategory = model.Subcategory;
        if (model.StudyGroup != null) entity.StudyGroup = model.StudyGroup;
        if (model.Tag1 != null) entity.Tag1 = model.Tag1;
        if (model.Tag2 != null) entity.Tag2 = model.Tag2;
        if (model.Tag3 != null) entity.Tag3 = model.Tag3;
        if (model.Tag4 != null) entity.Tag4 = model.Tag4;
        if (model.Comment != null) entity.Comment = model.Comment;
        if (model.Description != null) entity.Description = model.Description;
        if (model.LaunchYear.HasValue) entity.LaunchYear = model.LaunchYear;
        if (model.StudyCurrency != null) entity.StudyCurrency = model.StudyCurrency;
        if (model.Finances != null)
        {
            entity.FinanceType = model.Finances.FinanceType;
            entity.AccountingCode1 = model.Finances.AccountingCode1;
            entity.AccountingCode2 = model.Finances.AccountingCode2;
            entity.AccountingCode3 = model.Finances.AccountingCode3;
            entity.AccountingCode4 = model.Finances.AccountingCode4;
        }
        if (model.OpportunityDetails != null)
        {
            entity.OpportunityLevel = model.OpportunityDetails.OpportunityLevel;
            entity.OpportunityProbability = model.OpportunityDetails.Probability;
            entity.OpportunityExpectedDate = ToUtc(model.OpportunityDetails.ExpectedDate);
            entity.OpportunityExpectedNumberOfSites = model.OpportunityDetails.ExpectedNumberOfSites;
            entity.OpportunityComment = model.OpportunityDetails.Comment;
        }
        if (model.EnrollmentNote != null) entity.EnrollmentNote = model.EnrollmentNote;
        if (model.BudgetNote != null) entity.BudgetNote = model.BudgetNote;
        if (model.RegulatoryNote != null) entity.RegulatoryNote = model.RegulatoryNote;
        if (model.ContractNote != null) entity.ContractNote = model.ContractNote;
        if (model.StudyLead != null)
        {
            entity.LeadSourceStaffId = model.StudyLead.StaffId;
            entity.LeadSource = model.StudyLead.Source;
            entity.LeadDate = ToUtc(model.StudyLead.Date);
            entity.LeadComment = model.StudyLead.Comment;
        }
        if (model.Uid.HasValue) entity.Uid = model.Uid.Value;

        if (model.TargetDates != null)
        {
            entity.TargetDates.Clear();
            foreach (var t in model.TargetDates)
                entity.TargetDates.Add(new StudyTargetDate { Name = t.Name, Tooltip = t.Tooltip, Required = t.Required, TargetDate = ToUtc(t.Date) });
        }
        if (model.Leadership != null)
        {
            entity.Leadership.Clear();
            foreach (var l in model.Leadership)
                entity.Leadership.Add(new StudyLeadership { Name = l.Name, Required = l.Required, StaffId = l.StaffId });
        }
        if (model.CustomFields != null)
        {
            entity.CustomFieldValues.Clear();
            foreach (var c in model.CustomFields)
                entity.CustomFieldValues.Add(new StudyCustomFieldValue { FieldName = c.FieldName, FieldValue = c.FieldValue });
        }
        if (model.Contacts != null)
        {
            foreach (var c in model.Contacts) ApplyContactEntry(entity, c);
        }
    }

    // Upserts by (Type, Slot) in place rather than replacing, so a PATCH that omits a contact
    // slot leaves it untouched. PUT (StudiesController) clears Contacts first and calls
    // SyncContactsFromEdit instead, since a full replace must clear slots the caller didn't
    // send. Mirrors PatientMappingService.ApplyPhoneSlot / PatientsController.SyncPhonesFromEdit.
    private static void ApplyContactEntry(Study entity, StudyContactEntryEditModel model)
    {
        var contact = entity.Contacts.FirstOrDefault(c => c.ContactType == model.Type && c.Slot == model.Slot);
        if (contact == null)
        {
            entity.Contacts.Add(new StudyContact { ContactType = model.Type, Slot = model.Slot, Name = model.Name, Reference = model.Reference, Comment = model.Comment });
        }
        else
        {
            contact.Name = model.Name;
            contact.Reference = model.Reference;
            contact.Comment = model.Comment;
        }
    }

    public static StaffPreviewModel? ToStaffPreview(Staff? staff) => staff == null
        ? null
        : new StaffPreviewModel
        {
            Id = staff.Id,
            Uid = staff.StaffUid,
            Login = $"{staff.FirstName}.{staff.LastName}".ToLowerInvariant(),
            FirstName = staff.FirstName,
            LastName = staff.LastName,
            DisplayName = $"{staff.LastName}, {staff.FirstName}"
        };

    // ---- Structural sub-resources ----

    public static StudyArmViewModel ToViewModel(StudyArm a) => new()
    {
        Id = a.Id,
        Uid = a.Uid,
        Study = ToPreview(a.Study),
        ProtocolVersion = a.ProtocolVersion == null ? null : ToPreview(a.ProtocolVersion),
        Name = a.Name,
        Status = a.Status,
        PatientGoal = a.PatientGoal,
        PatientLimit = a.PatientLimit,
        Comment = a.Comment,
        ImportId = a.ImportId,
        ImportType = a.ImportType
    };

    public static void ApplyEditModel(StudyArm entity, StudyArmEditModel model)
    {
        entity.ProtocolVersionId = model.ProtocolVersionId;
        entity.Name = model.Name;
        entity.Status = model.Status;
        entity.PatientGoal = model.PatientGoal;
        entity.PatientLimit = model.PatientLimit;
        entity.Comment = model.Comment;
        entity.ImportId = model.ImportId;
        entity.ImportType = model.ImportType;
    }

    public static StudyArmPreviewModel ToPreview(StudyArm a) => new() { Id = a.Id, Uid = a.Uid, Name = a.Name };

    public static StudyVisitViewModel ToViewModel(StudyVisit v) => new()
    {
        Id = v.Id,
        Uid = v.Uid,
        Study = ToPreview(v.Study),
        ProtocolVersion = v.ProtocolVersion == null ? null : ToPreview(v.ProtocolVersion),
        Arms = v.VisitArms.Where(va => va.StudyArm != null).Select(va => ToPreview(va.StudyArm)).ToList(),
        Name = v.Name,
        Type = v.Type,
        Reference = v.Reference,
        OptionalProcedure = v.OptionalProcedure,
        Description = v.Description,
        StandardMinutes = v.StandardMinutes,
        Budget = v.Budget,
        Cost = v.Cost,
        IsBudgetAutoRecomputed = v.IsBudgetAutoRecomputed,
        IsCostAutoRecomputed = v.IsCostAutoRecomputed,
        PatientStipend = v.PatientStipend,
        CaregiverStipend = v.CaregiverStipend,
        IsActive = v.IsActive,
        AutoRepeat = v.AutoRepeat,
        RepeatOnDemand = v.RepeatOnDemand,
        ImportId = v.ImportId,
        ImportType = v.ImportType
    };

    public static void ApplyEditModel(StudyVisit entity, StudyVisitEditModel model)
    {
        entity.ProtocolVersionId = model.ProtocolVersionId;
        entity.Name = model.Name;
        entity.Type = model.Type;
        entity.Reference = model.Reference;
        entity.OptionalProcedure = model.OptionalProcedure;
        entity.Description = model.Description;
        entity.StandardMinutes = model.StandardMinutes;
        entity.Budget = model.Budget;
        entity.Cost = model.Cost;
        entity.IsBudgetAutoRecomputed = model.IsBudgetAutoRecomputed;
        entity.IsCostAutoRecomputed = model.IsCostAutoRecomputed;
        entity.PatientStipend = model.PatientStipend;
        entity.CaregiverStipend = model.CaregiverStipend;
        entity.IsActive = model.IsActive;
        entity.AutoRepeat = model.AutoRepeat;
        entity.RepeatOnDemand = model.RepeatOnDemand;
        entity.ImportId = model.ImportId;
        entity.ImportType = model.ImportType;
    }

    public static StudyMilestoneViewModel ToViewModel(StudyMilestone m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Category = m.Category,
        Importance = m.Importance,
        Status = m.Status,
        Comment = m.Comment,
        AssignedTo = ToStaffPreview(m.AssignedToStaff),
        AssignedOn = m.AssignedOn,
        ProjectedDate = m.ProjectedDate,
        CompletedOn = m.CompletedOn,
        HasAutoExpenditure = m.HasAutoExpenditure,
        Scheduling = new MilestoneScheduleViewModel
        {
            SchedulingMode = m.SchedulingMode,
            DueDate = m.DueDate,
            Offset = m.Offset,
            OffsetUnits = m.OffsetUnits,
            WindowMin = m.WindowMin,
            WindowMax = m.WindowMax,
            WindowUnits = m.WindowUnits
        }
    };

    public static void ApplyEditModel(StudyMilestone entity, StudyMilestoneEditModel model)
    {
        entity.Name = model.Name;
        entity.Category = model.Category;
        entity.Importance = model.Importance;
        entity.Status = model.Status;
        entity.Comment = model.Comment;
        entity.AssignedToStaffId = model.AssignedToStaffId;
        entity.AssignedOn = ToUtc(model.AssignedOn);
        entity.ProjectedDate = ToUtc(model.ProjectedDate);
        entity.CompletedOn = ToUtc(model.CompletedOn);
        entity.HasAutoExpenditure = model.HasAutoExpenditure;
        entity.SchedulingMode = model.Scheduling?.SchedulingMode;
        entity.DueDate = ToUtc(model.Scheduling?.DueDate);
        entity.Offset = model.Scheduling?.Offset;
        entity.OffsetUnits = model.Scheduling?.OffsetUnits;
        entity.WindowMin = model.Scheduling?.WindowMin;
        entity.WindowMax = model.Scheduling?.WindowMax;
        entity.WindowUnits = model.Scheduling?.WindowUnits;
    }

    public static StudyDocumentViewModel ToViewModel(StudyDocument d) => new()
    {
        Id = d.Id,
        Uid = d.Uid,
        TypeName = d.TypeName,
        TypeCategory = d.TypeCategory,
        StatusName = d.StatusName,
        Description = d.Description,
        Version = d.Version,
        Source = d.Source,
        EffectiveDate = d.EffectiveDate,
        ExpirationDate = d.ExpirationDate
    };

    public static void ApplyEditModel(StudyDocument entity, StudyDocumentEditModel model)
    {
        entity.TypeName = model.TypeName;
        entity.TypeCategory = model.TypeCategory;
        entity.StatusName = model.StatusName;
        entity.Description = model.Description;
        entity.Version = model.Version;
        entity.Source = model.Source;
        entity.EffectiveDate = ToUtc(model.EffectiveDate);
        entity.ExpirationDate = ToUtc(model.ExpirationDate);
    }

    public static StudyDocumentStatusHistoryViewModel ToViewModel(StudyDocumentStatusHistory h) => new()
    {
        Id = h.Id,
        StatusName = h.StatusName,
        ChangedOn = h.ChangedOn,
        ChangedBy = ToStaffPreview(h.ChangedByStaff),
        Comment = h.Comment
    };

    public static StudyNoteViewModel ToViewModel(StudyNote n) => new()
    {
        Id = n.Id,
        Staff = ToStaffPreview(n.Staff),
        LastUpdatedStaff = ToStaffPreview(n.LastUpdatedStaff),
        Date = n.NoteDate,
        Note = n.Note,
        Locked = n.Locked,
        Shared = n.Shared
    };

    public static void ApplyEditModel(StudyNote entity, StudyNoteEditModel model)
    {
        entity.StaffId = model.StaffId;
        entity.LastUpdatedStaffId = model.LastUpdatedStaffId;
        entity.NoteDate = ToUtc(model.Date) ?? entity.NoteDate;
        entity.Note = model.Note;
        entity.Locked = model.Locked;
        entity.Shared = model.Shared;
    }

    public static StudyRoleViewModel ToViewModel(StudyRole r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        IsCoordinator = r.IsCoordinator,
        AllowRoleSharing = r.AllowRoleSharing,
        RestrictReassignment = r.RestrictReassignment,
        Staff = r.RoleStaff
            .Where(rs => rs.Staff != null)
            .Select(rs => new StudyRoleStaffViewModel { Staff = ToStaffPreview(rs.Staff)!, Priority = rs.Priority })
            .ToList()
    };

    public static ProtocolVersionViewModel ToViewModel(ProtocolVersion pv) => new()
    {
        Id = pv.Id,
        Uid = pv.Uid,
        Study = ToPreview(pv.Study),
        Name = pv.Name,
        VersionDate = pv.VersionDate,
        TreatmentStatus = pv.TreatmentStatus,
        Status = pv.Status,
        ProtocolNumber = pv.ProtocolNumber,
        Comment = pv.Comment,
        IrbApprovalDate = pv.IrbApprovalDate,
        IsPatientReconsentRequired = pv.IsPatientReconsentRequired,
        ImportId = pv.ImportId,
        ImportType = pv.ImportType
    };

    public static void ApplyEditModel(ProtocolVersion entity, ProtocolVersionEditModel model)
    {
        entity.Name = model.Name;
        entity.VersionDate = ToUtc(model.VersionDate);
        entity.TreatmentStatus = model.TreatmentStatus;
        entity.Status = model.Status;
        entity.ProtocolNumber = model.ProtocolNumber;
        entity.Comment = model.Comment;
        entity.IrbApprovalDate = ToUtc(model.IrbApprovalDate);
        entity.IsPatientReconsentRequired = model.IsPatientReconsentRequired;
        entity.ImportId = model.ImportId;
        entity.ImportType = model.ImportType;
    }

    public static ProtocolVersionPreviewModel ToPreview(ProtocolVersion pv) => new() { Id = pv.Id, Uid = pv.Uid, Name = pv.Name };

    // ---- Reference / lookup data ----

    public static StudyCategoryViewModel ToViewModel(StudyCategory c) => new() { Id = c.Id, Name = c.Name, Description = c.Description };

    public static StudySubcategoryViewModel ToViewModel(StudySubcategory s) => new()
    {
        Id = s.Id,
        StudyCategoryId = s.StudyCategoryId,
        Name = s.Name,
        Description = s.Description
    };

    public static StudyTypeViewModel ToViewModel(StudyType t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Description = t.Description,
        ForeColor = t.ForeColor,
        BackColor = t.BackColor
    };

    public static StudyStatusTypeViewModel ToViewModel(StudyStatusType s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        BackColor = s.BackColor,
        IsActive = s.IsActive,
        IsEnrollmentPermitted = s.IsEnrollmentPermitted,
        StudyPhase = s.StudyPhase
    };

    public static StudyGroupViewModel ToViewModel(StudyGroup g) => new() { Id = g.Id, Name = g.Name };
}
