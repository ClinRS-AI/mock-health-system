namespace MockHealthSystem.Infrastructure.Data.Entities;

public class Study
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

    public int SponsorTeamId { get; set; }
    public int? ManagingSiteId { get; set; }

    public string? FinanceType { get; set; }
    public string? AccountingCode1 { get; set; }
    public string? AccountingCode2 { get; set; }
    public string? AccountingCode3 { get; set; }
    public string? AccountingCode4 { get; set; }

    public string? OpportunityLevel { get; set; }
    public double? OpportunityProbability { get; set; }
    public DateTime? OpportunityExpectedDate { get; set; }
    public int? OpportunityExpectedNumberOfSites { get; set; }
    public string? OpportunityComment { get; set; }

    public string? EnrollmentNote { get; set; }
    public string? BudgetNote { get; set; }
    public string? RegulatoryNote { get; set; }
    public string? ContractNote { get; set; }

    public int? LeadSourceStaffId { get; set; }
    public string? LeadSource { get; set; }
    public DateTime? LeadDate { get; set; }
    public string? LeadComment { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime LastUpdatedOn { get; set; }

    public SponsorTeam SponsorTeam { get; set; } = null!;
    public Site? ManagingSite { get; set; }
    public Staff? LeadSourceStaff { get; set; }

    public ICollection<StudyArm> Arms { get; set; } = new List<StudyArm>();
    public ICollection<StudyVisit> Visits { get; set; } = new List<StudyVisit>();
    public ICollection<StudyMilestone> Milestones { get; set; } = new List<StudyMilestone>();
    public ICollection<StudyDocument> Documents { get; set; } = new List<StudyDocument>();
    public ICollection<StudyNote> Notes { get; set; } = new List<StudyNote>();
    public ICollection<StudyRole> Roles { get; set; } = new List<StudyRole>();
    public ICollection<ProtocolVersion> ProtocolVersions { get; set; } = new List<ProtocolVersion>();
    public ICollection<StudyTargetDate> TargetDates { get; set; } = new List<StudyTargetDate>();
    public ICollection<StudyLeadership> Leadership { get; set; } = new List<StudyLeadership>();
    public ICollection<StudyCustomFieldValue> CustomFieldValues { get; set; } = new List<StudyCustomFieldValue>();
    public ICollection<StudyContact> Contacts { get; set; } = new List<StudyContact>();
    public ICollection<StudyStudyType> StudyTypes { get; set; } = new List<StudyStudyType>();
}
