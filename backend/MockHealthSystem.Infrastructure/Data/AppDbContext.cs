using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientPhone> PatientPhones => Set<PatientPhone>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<PatientMedicalDevice> PatientMedicalDevices => Set<PatientMedicalDevice>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<ProviderType> ProviderTypes => Set<ProviderType>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<PatientProvider> PatientProviders => Set<PatientProvider>();
    public DbSet<Condition> Conditions => Set<Condition>();
    public DbSet<Gender> Genders => Set<Gender>();
    public DbSet<PatientCondition> PatientConditions => Set<PatientCondition>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<PatientProcedure> PatientProcedures => Set<PatientProcedure>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<MedicationRoute> MedicationRoutes => Set<MedicationRoute>();
    public DbSet<MedicationType> MedicationTypes => Set<MedicationType>();
    public DbSet<MedicationSchedule> MedicationSchedules => Set<MedicationSchedule>();
    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();
    public DbSet<PatientMedicationCondition> PatientMedicationConditions => Set<PatientMedicationCondition>();
    public DbSet<Immunization> Immunizations => Set<Immunization>();
    public DbSet<ImmunizationType> ImmunizationTypes => Set<ImmunizationType>();
    public DbSet<PatientImmunization> PatientImmunizations => Set<PatientImmunization>();
    public DbSet<Relation> Relations => Set<Relation>();
    public DbSet<PatientFamilyHistory> PatientFamilyHistories => Set<PatientFamilyHistory>();
    public DbSet<ConditionType> ConditionTypes => Set<ConditionType>();
    public DbSet<SocialHistory> SocialHistories => Set<SocialHistory>();
    public DbSet<AllergenType> AllergenTypes => Set<AllergenType>();
    public DbSet<PatientSocialHistoryEntry> PatientSocialHistoryEntries => Set<PatientSocialHistoryEntry>();

    public DbSet<AuthSettings> AuthSettings => Set<AuthSettings>();
    public DbSet<AuthToken> AuthTokens => Set<AuthToken>();
    public DbSet<ApiRequestLog> ApiRequestLogs => Set<ApiRequestLog>();
    public DbSet<ReportQueryDefinition> ReportQueryDefinitions => Set<ReportQueryDefinition>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<AuditEntryType> AuditEntryTypes => Set<AuditEntryType>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Sponsor> Sponsors => Set<Sponsor>();
    public DbSet<SponsorDivision> SponsorDivisions => Set<SponsorDivision>();
    public DbSet<SponsorTeam> SponsorTeams => Set<SponsorTeam>();
    public DbSet<StudyCategory> StudyCategories => Set<StudyCategory>();
    public DbSet<StudySubcategory> StudySubcategories => Set<StudySubcategory>();
    public DbSet<StudyType> StudyTypes => Set<StudyType>();
    public DbSet<StudyStatusType> StudyStatusTypes => Set<StudyStatusType>();
    public DbSet<StudyGroup> StudyGroups => Set<StudyGroup>();
    public DbSet<Study> Studies => Set<Study>();
    public DbSet<StudyArm> StudyArms => Set<StudyArm>();
    public DbSet<StudyVisit> StudyVisits => Set<StudyVisit>();
    public DbSet<StudyVisitArm> StudyVisitArms => Set<StudyVisitArm>();
    public DbSet<StudyMilestone> StudyMilestones => Set<StudyMilestone>();
    public DbSet<StudyDocument> StudyDocuments => Set<StudyDocument>();
    public DbSet<StudyDocumentStatusHistory> StudyDocumentStatusHistories => Set<StudyDocumentStatusHistory>();
    public DbSet<StudyContact> StudyContacts => Set<StudyContact>();
    public DbSet<StudyNote> StudyNotes => Set<StudyNote>();
    public DbSet<StudyRole> StudyRoles => Set<StudyRole>();
    public DbSet<StudyRoleStaff> StudyRoleStaffs => Set<StudyRoleStaff>();
    public DbSet<ProtocolVersion> ProtocolVersions => Set<ProtocolVersion>();
    public DbSet<StudyTargetDate> StudyTargetDates => Set<StudyTargetDate>();
    public DbSet<StudyLeadership> StudyLeaderships => Set<StudyLeadership>();
    public DbSet<StudyCustomFieldValue> StudyCustomFieldValues => Set<StudyCustomFieldValue>();
    public DbSet<StudyStudyType> StudyStudyTypes => Set<StudyStudyType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientMedicationCondition>()
            .HasKey(x => new { x.PatientMedicationId, x.PatientConditionId });

        modelBuilder.Entity<PatientMedicationCondition>()
            .HasOne(x => x.PatientMedication)
            .WithMany(m => m.MedicationConditions)
            .HasForeignKey(x => x.PatientMedicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientMedicationCondition>()
            .HasOne(x => x.PatientCondition)
            .WithMany(c => c.MedicationConditions)
            .HasForeignKey(x => x.PatientConditionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientFamilyHistory>()
            .HasOne(x => x.Relation)
            .WithMany()
            .HasForeignKey(x => x.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuthSettings>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<AuthSettings>()
            .Property(x => x.Mode)
            .HasMaxLength(20);

        modelBuilder.Entity<AuthSettings>()
            .HasData(new AuthSettings
            {
                Id = 1,
                Mode = "None",
                AccessTokenLifetimeMinutes = 60,
                RefreshTokenLifetimeDays = 30
            });

        modelBuilder.Entity<AuthToken>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<AuthToken>()
            .Property(x => x.Token)
            .IsRequired();

        modelBuilder.Entity<AuthToken>()
            .Property(x => x.TokenType)
            .IsRequired()
            .HasMaxLength(16);

        modelBuilder.Entity<AuthToken>()
            .Property(x => x.ClientId)
            .IsRequired()
            .HasMaxLength(128);

        modelBuilder.Entity<AuthToken>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<ApiRequestLog>()
            .HasIndex(x => x.CreatedAtUtc);

        modelBuilder.Entity<ApiRequestLog>()
            .HasIndex(x => x.Path);

        modelBuilder.Entity<ApiRequestLog>()
            .HasIndex(x => x.StatusCode);

        modelBuilder.Entity<ReportQueryDefinition>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<ReportQueryDefinition>()
            .Property(x => x.PKey)
            .IsRequired()
            .HasMaxLength(128);

        modelBuilder.Entity<ReportQueryDefinition>()
            .Property(x => x.SqlQuery)
            .IsRequired();

        modelBuilder.Entity<ReportQueryDefinition>()
            .HasIndex(x => x.PKey)
            .IsUnique();

        modelBuilder.Entity<ReportQueryDefinition>()
            .Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<ReportQueryDefinition>()
            .Property(x => x.UpdatedAtUtc)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<Staff>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Staff>()
            .Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(128);

        modelBuilder.Entity<Staff>()
            .Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(128);

        modelBuilder.Entity<AuditEntryType>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<AuditEntryType>()
            .Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(64);

        modelBuilder.Entity<AuditEntryType>()
            .Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        modelBuilder.Entity<AuditEntryType>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<AuditLog>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<AuditLog>()
            .Property(x => x.CreatedByUser)
            .IsRequired()
            .HasMaxLength(256);

        modelBuilder.Entity<AuditLog>()
            .Property(x => x.StudyPKey)
            .HasMaxLength(128);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => x.CreatedTimeUtc);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => x.StaffPKey);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => x.PatientPKey);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => x.AuditEntryTypeId);

        modelBuilder.Entity<AuditLog>()
            .HasOne(x => x.Staff)
            .WithMany()
            .HasForeignKey(x => x.StaffPKey)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientPKey)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>()
            .HasOne(x => x.AuditEntryType)
            .WithMany()
            .HasForeignKey(x => x.AuditEntryTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Study domain ----

        modelBuilder.Entity<Sponsor>().HasIndex(x => x.Uid).IsUnique();
        modelBuilder.Entity<SponsorDivision>()
            .HasOne(x => x.Sponsor).WithMany().HasForeignKey(x => x.SponsorId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SponsorTeam>()
            .HasOne(x => x.SponsorDivision).WithMany().HasForeignKey(x => x.SponsorDivisionId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudyCategory>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<StudySubcategory>()
            .HasOne(x => x.StudyCategory).WithMany().HasForeignKey(x => x.StudyCategoryId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<StudyType>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<StudyStatusType>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<StudyGroup>().HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<Study>().HasIndex(x => x.Uid).IsUnique();
        modelBuilder.Entity<Study>()
            .HasOne(x => x.SponsorTeam).WithMany().HasForeignKey(x => x.SponsorTeamId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Study>()
            .HasOne(x => x.ManagingSite).WithMany().HasForeignKey(x => x.ManagingSiteId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Study>()
            .HasOne(x => x.LeadSourceStaff).WithMany().HasForeignKey(x => x.LeadSourceStaffId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StudyArm>().HasIndex(x => x.Uid).IsUnique();
        modelBuilder.Entity<StudyArm>()
            .HasOne(x => x.Study).WithMany(s => s.Arms).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyArm>()
            .HasOne(x => x.ProtocolVersion).WithMany().HasForeignKey(x => x.ProtocolVersionId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudyVisit>().HasIndex(x => x.Uid).IsUnique();
        modelBuilder.Entity<StudyVisit>()
            .HasOne(x => x.Study).WithMany(s => s.Visits).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyVisit>()
            .HasOne(x => x.ProtocolVersion).WithMany().HasForeignKey(x => x.ProtocolVersionId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudyVisitArm>().HasKey(x => new { x.VisitId, x.ArmId });
        modelBuilder.Entity<StudyVisitArm>()
            .HasOne(x => x.StudyVisit).WithMany(v => v.VisitArms).HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyVisitArm>()
            .HasOne(x => x.StudyArm).WithMany(a => a.VisitArms).HasForeignKey(x => x.ArmId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudyMilestone>()
            .HasOne(x => x.Study).WithMany(s => s.Milestones).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyMilestone>()
            .HasOne(x => x.AssignedToStaff).WithMany().HasForeignKey(x => x.AssignedToStaffId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StudyDocument>().HasIndex(x => x.Uid).IsUnique();
        modelBuilder.Entity<StudyDocument>()
            .HasOne(x => x.Study).WithMany(s => s.Documents).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyDocumentStatusHistory>()
            .HasOne(x => x.StudyDocument).WithMany(d => d.StatusHistory).HasForeignKey(x => x.StudyDocumentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyDocumentStatusHistory>()
            .HasOne(x => x.ChangedByStaff).WithMany().HasForeignKey(x => x.ChangedByStaffId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StudyContact>()
            .HasOne(x => x.Study).WithMany(s => s.Contacts).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyContact>()
            .HasIndex(x => new { x.StudyId, x.ContactType, x.Slot }).IsUnique();

        modelBuilder.Entity<StudyNote>()
            .HasOne(x => x.Study).WithMany(s => s.Notes).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyNote>()
            .HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<StudyNote>()
            .HasOne(x => x.LastUpdatedStaff).WithMany().HasForeignKey(x => x.LastUpdatedStaffId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StudyRole>()
            .HasOne(x => x.Study).WithMany(s => s.Roles).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyRoleStaff>().HasKey(x => new { x.StudyRoleId, x.StaffId });
        modelBuilder.Entity<StudyRoleStaff>()
            .HasOne(x => x.StudyRole).WithMany(r => r.RoleStaff).HasForeignKey(x => x.StudyRoleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyRoleStaff>()
            .HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProtocolVersion>().HasIndex(x => x.Uid).IsUnique();
        modelBuilder.Entity<ProtocolVersion>()
            .HasOne(x => x.Study).WithMany(s => s.ProtocolVersions).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudyTargetDate>()
            .HasOne(x => x.Study).WithMany(s => s.TargetDates).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudyLeadership>()
            .HasOne(x => x.Study).WithMany(s => s.Leadership).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyLeadership>()
            .HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StudyCustomFieldValue>()
            .HasOne(x => x.Study).WithMany(s => s.CustomFieldValues).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudyStudyType>().HasKey(x => new { x.StudyId, x.StudyTypeId });
        modelBuilder.Entity<StudyStudyType>()
            .HasOne(x => x.Study).WithMany(s => s.StudyTypes).HasForeignKey(x => x.StudyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudyStudyType>()
            .HasOne(x => x.StudyType).WithMany().HasForeignKey(x => x.StudyTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
