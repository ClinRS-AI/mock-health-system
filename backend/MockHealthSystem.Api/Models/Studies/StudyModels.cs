using MockHealthSystem.Api.Models.Patients;

namespace MockHealthSystem.Api.Models.Studies;

public class StudyViewModel
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Identifier { get; set; }
    public string? ProtocolNumber { get; set; }
    public string? IndIdeNumber { get; set; }
    public string? NctNumber { get; set; }
    public string? Phase { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public string? StudyGroup { get; set; }
    public string? Tag1 { get; set; }
    public string? Tag2 { get; set; }
    public string? Tag3 { get; set; }
    public string? Tag4 { get; set; }
    public string? Comment { get; set; }
    public string? Description { get; set; }
    public int? LaunchYear { get; set; }
    public string? StudyCurrency { get; set; }

    public SponsorTeamPreviewModel SponsorTeam { get; set; } = null!;
    public SitePreviewModel? ManagingSite { get; set; }

    public StudyFinanceModel? Finances { get; set; }
    public StudyOpportunityModel? OpportunityDetails { get; set; }

    public string? EnrollmentNote { get; set; }
    public string? BudgetNote { get; set; }
    public string? RegulatoryNote { get; set; }
    public string? ContractNote { get; set; }

    public StudyLeadSourceViewModel? StudyLead { get; set; }
    public IList<StudyTargetDateViewModel> TargetDates { get; set; } = new List<StudyTargetDateViewModel>();
    public IList<StudyLeaderViewModel> Leadership { get; set; } = new List<StudyLeaderViewModel>();
    public IList<StudyCustomFieldModel> CustomFields { get; set; } = new List<StudyCustomFieldModel>();
    public IList<StudyContactEntryViewModel> Contacts { get; set; } = new List<StudyContactEntryViewModel>();

    public DateTime CreatedOn { get; set; }
    public DateTime LastUpdatedOn { get; set; }
}

public class StudyEditModel
{
    public int SponsorTeamId { get; set; }
    public int? ManagingSiteId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Identifier { get; set; }
    public string? ProtocolNumber { get; set; }
    public string? IndIdeNumber { get; set; }
    public string? NctNumber { get; set; }
    public string? Phase { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public string? StudyGroup { get; set; }
    public string? Tag1 { get; set; }
    public string? Tag2 { get; set; }
    public string? Tag3 { get; set; }
    public string? Tag4 { get; set; }
    public string? Comment { get; set; }
    public string? Description { get; set; }
    public int? LaunchYear { get; set; }
    public string? StudyCurrency { get; set; }

    public StudyFinanceModel? Finances { get; set; }
    public StudyOpportunityModel? OpportunityDetails { get; set; }

    public string? EnrollmentNote { get; set; }
    public string? BudgetNote { get; set; }
    public string? RegulatoryNote { get; set; }
    public string? ContractNote { get; set; }

    public StudyLeadSourceEditModel? StudyLead { get; set; }
    public IList<StudyTargetDateEditModel>? TargetDates { get; set; }
    public IList<StudyLeaderEditModel>? Leadership { get; set; }
    public IList<StudyCustomFieldModel>? CustomFields { get; set; }
    public IList<StudyContactEntryEditModel>? Contacts { get; set; }

    public Guid? Uid { get; set; }
}

/// <summary>
/// Partial update model for study (PATCH). All fields optional; omitted fields
/// (including omitted embedded array entries/slots) are left untouched.
/// </summary>
public class StudyPatchModel
{
    public int? SponsorTeamId { get; set; }
    public int? ManagingSiteId { get; set; }

    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Identifier { get; set; }
    public string? ProtocolNumber { get; set; }
    public string? IndIdeNumber { get; set; }
    public string? NctNumber { get; set; }
    public string? Phase { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public string? StudyGroup { get; set; }
    public string? Tag1 { get; set; }
    public string? Tag2 { get; set; }
    public string? Tag3 { get; set; }
    public string? Tag4 { get; set; }
    public string? Comment { get; set; }
    public string? Description { get; set; }
    public int? LaunchYear { get; set; }
    public string? StudyCurrency { get; set; }

    public StudyFinanceModel? Finances { get; set; }
    public StudyOpportunityModel? OpportunityDetails { get; set; }

    public string? EnrollmentNote { get; set; }
    public string? BudgetNote { get; set; }
    public string? RegulatoryNote { get; set; }
    public string? ContractNote { get; set; }

    public StudyLeadSourceEditModel? StudyLead { get; set; }
    public IList<StudyTargetDateEditModel>? TargetDates { get; set; }
    public IList<StudyLeaderEditModel>? Leadership { get; set; }
    public IList<StudyCustomFieldModel>? CustomFields { get; set; }
    public IList<StudyContactEntryEditModel>? Contacts { get; set; }

    public Guid? Uid { get; set; }
}

public class SponsorTeamPreviewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class StaffPreviewModel
{
    public int Id { get; set; }
    public Guid? Uid { get; set; }
    public string? Login { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
}

public class StudyFinanceModel
{
    public string? FinanceType { get; set; }
    public string? AccountingCode1 { get; set; }
    public string? AccountingCode2 { get; set; }
    public string? AccountingCode3 { get; set; }
    public string? AccountingCode4 { get; set; }
}

public class StudyOpportunityModel
{
    public string? OpportunityLevel { get; set; }
    public double? Probability { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public int? ExpectedNumberOfSites { get; set; }
    public string? Comment { get; set; }
}

public class StudyLeadSourceViewModel
{
    public StaffPreviewModel? Staff { get; set; }
    public string? Source { get; set; }
    public DateTime? Date { get; set; }
    public string? Comment { get; set; }
}

public class StudyLeadSourceEditModel
{
    public int? StaffId { get; set; }
    public string? Source { get; set; }
    public DateTime? Date { get; set; }
    public string? Comment { get; set; }
}

public class StudyTargetDateViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Tooltip { get; set; }
    public bool Required { get; set; }
    public DateTime? Date { get; set; }
}

public class StudyTargetDateEditModel
{
    public string Name { get; set; } = string.Empty;
    public string? Tooltip { get; set; }
    public bool Required { get; set; }
    public DateTime? Date { get; set; }
}

public class StudyLeaderViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; }
    public StaffPreviewModel? Staff { get; set; }
}

public class StudyLeaderEditModel
{
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; }
    public int? StaffId { get; set; }
}

public class StudyCustomFieldModel
{
    public string FieldName { get; set; } = string.Empty;
    public string? FieldValue { get; set; }
}

/// <summary>Contact type: Irb | Cro | Lab | Monitor | Vendor. Slot: 1-2 per type.</summary>
public class StudyContactEntryViewModel
{
    public string Type { get; set; } = string.Empty;
    public int Slot { get; set; }
    public string? Name { get; set; }
    public string? Reference { get; set; }
    public string? Comment { get; set; }
}

public class StudyContactEntryEditModel
{
    public string Type { get; set; } = string.Empty;
    public int Slot { get; set; }
    public string? Name { get; set; }
    public string? Reference { get; set; }
    public string? Comment { get; set; }
}
