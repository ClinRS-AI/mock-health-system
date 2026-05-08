using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

/// <summary>
/// Asserts that AppDbContext's OnModelCreating wires up the relational contracts that runtime code
/// depends on (composite keys, delete behaviors, unique indexes, max lengths). These are the
/// metadata branches that the rest of the integration suite never directly touches.
/// </summary>
public sealed class AppDbContextModelConfigurationTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ModelTests_{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void PatientMedicationCondition_HasCompositePrimaryKey()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(PatientMedicationCondition))!;
        var key = entity.FindPrimaryKey()!;

        Assert.Equal(2, key.Properties.Count);
        Assert.Contains(key.Properties, p => p.Name == nameof(PatientMedicationCondition.PatientMedicationId));
        Assert.Contains(key.Properties, p => p.Name == nameof(PatientMedicationCondition.PatientConditionId));
    }

    [Fact]
    public void PatientMedicationCondition_DeleteBehavior_IsCascade_ForBothForeignKeys()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(PatientMedicationCondition))!;

        var medFk = entity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(PatientMedication));
        var condFk = entity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(PatientCondition));

        Assert.Equal(DeleteBehavior.Cascade, medFk.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, condFk.DeleteBehavior);
    }

    [Fact]
    public void PatientFamilyHistory_RelationDeleteBehavior_IsRestrict()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(PatientFamilyHistory))!;
        var relationFk = entity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Relation));

        Assert.Equal(DeleteBehavior.Restrict, relationFk.DeleteBehavior);
    }

    [Fact]
    public void AuditLog_StaffAndPatientForeignKeys_UseSetNull_AndAuditEntryTypeUsesRestrict()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(AuditLog))!;

        var staffFk = entity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Staff));
        var patientFk = entity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Patient));
        var typeFk = entity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(AuditEntryType));

        Assert.Equal(DeleteBehavior.SetNull, staffFk.DeleteBehavior);
        Assert.Equal(DeleteBehavior.SetNull, patientFk.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, typeFk.DeleteBehavior);
    }

    [Fact]
    public void AuthToken_HasUniqueIndexOnToken()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(AuthToken))!;
        var index = entity.GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(AuthToken.Token));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void ReportQueryDefinition_HasUniqueIndexOnPKey()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(ReportQueryDefinition))!;
        var index = entity.GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(ReportQueryDefinition.PKey));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void AuditEntryType_HasUniqueIndexOnCode()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(AuditEntryType))!;
        var index = entity.GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(AuditEntryType.Code));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void AuthSettings_ModeProperty_HasMaxLength20()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(AuthSettings))!;
        var modeProp = entity.FindProperty(nameof(AuthSettings.Mode))!;

        Assert.Equal(20, modeProp.GetMaxLength());
    }

    [Fact]
    public void AuthToken_TokenType_AndClientId_HaveExpectedMaxLengths()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(AuthToken))!;

        Assert.Equal(16, entity.FindProperty(nameof(AuthToken.TokenType))!.GetMaxLength());
        Assert.Equal(128, entity.FindProperty(nameof(AuthToken.ClientId))!.GetMaxLength());
    }

    [Fact]
    public void Staff_FirstNameAndLastName_HaveMaxLength128()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(Staff))!;

        Assert.Equal(128, entity.FindProperty(nameof(Staff.FirstName))!.GetMaxLength());
        Assert.Equal(128, entity.FindProperty(nameof(Staff.LastName))!.GetMaxLength());
    }

    [Fact]
    public void AuditEntryType_CodeAndDisplayName_HaveExpectedMaxLengths()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(AuditEntryType))!;

        Assert.Equal(64, entity.FindProperty(nameof(AuditEntryType.Code))!.GetMaxLength());
        Assert.Equal(256, entity.FindProperty(nameof(AuditEntryType.DisplayName))!.GetMaxLength());
    }

    [Fact]
    public void ApiRequestLog_HasIndexes_OnCreatedAtPathAndStatus()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(ApiRequestLog))!;

        Assert.Contains(entity.GetIndexes(), i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(ApiRequestLog.CreatedAtUtc));
        Assert.Contains(entity.GetIndexes(), i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(ApiRequestLog.Path));
        Assert.Contains(entity.GetIndexes(), i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(ApiRequestLog.StatusCode));
    }

    [Fact]
    public void AuditLog_HasIndexesOnCreatedTimeAndForeignKeys()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(AuditLog))!;

        Assert.Contains(entity.GetIndexes(), i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(AuditLog.CreatedTimeUtc));
        Assert.Contains(entity.GetIndexes(), i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(AuditLog.StaffPKey));
        Assert.Contains(entity.GetIndexes(), i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(AuditLog.PatientPKey));
        Assert.Contains(entity.GetIndexes(), i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(AuditLog.AuditEntryTypeId));
    }

    [Fact]
    public void AuthSettings_SeededRow_IsConfigured()
    {
        using var db = CreateDb();
        var designTimeModel = db.GetService<IDesignTimeModel>().Model;
        var entity = designTimeModel.FindEntityType(typeof(AuthSettings))!;
        var seedData = entity.GetSeedData().ToList();

        Assert.Single(seedData);
        var row = seedData[0];
        Assert.Equal(1, row[nameof(AuthSettings.Id)]);
        Assert.Equal("None", row[nameof(AuthSettings.Mode)]);
    }

    [Fact]
    public void AllExpectedDbSets_AreConfiguredOnModel()
    {
        using var db = CreateDb();

        // Touching each DbSet executes the property getters, providing a single source-of-truth
        // smoke test that all model entities are registered.
        var counts = new[]
        {
            db.Sites.Count(),
            db.Patients.Count(),
            db.PatientPhones.Count(),
            db.Devices.Count(),
            db.PatientMedicalDevices.Count(),
            db.Allergies.Count(),
            db.PatientAllergies.Count(),
            db.ProviderTypes.Count(),
            db.Providers.Count(),
            db.PatientProviders.Count(),
            db.Conditions.Count(),
            db.Genders.Count(),
            db.PatientConditions.Count(),
            db.Procedures.Count(),
            db.PatientProcedures.Count(),
            db.Medications.Count(),
            db.MedicationRoutes.Count(),
            db.MedicationTypes.Count(),
            db.MedicationSchedules.Count(),
            db.PatientMedications.Count(),
            db.PatientMedicationConditions.Count(),
            db.Immunizations.Count(),
            db.ImmunizationTypes.Count(),
            db.PatientImmunizations.Count(),
            db.Relations.Count(),
            db.PatientFamilyHistories.Count(),
            db.ConditionTypes.Count(),
            db.SocialHistories.Count(),
            db.AllergenTypes.Count(),
            db.PatientSocialHistoryEntries.Count(),
            db.AuthSettings.Count(),
            db.AuthTokens.Count(),
            db.ApiRequestLogs.Count(),
            db.ReportQueryDefinitions.Count(),
            db.Staff.Count(),
            db.AuditEntryTypes.Count(),
            db.AuditLogs.Count()
        };

        Assert.All(counts, c => Assert.True(c >= 0));
    }
}
