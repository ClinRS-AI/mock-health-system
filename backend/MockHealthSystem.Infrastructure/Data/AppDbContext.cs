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
    }
}
