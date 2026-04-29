using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class AuditLogModelTests
{
    [Fact]
    public void AuditEntryType_Code_HasUniqueIndex()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new AppDbContext(options);
        var entityType = db.Model.FindEntityType(typeof(AuditEntryType));
        Assert.NotNull(entityType);

        var codeIndex = entityType!.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(AuditEntryType.Code));

        Assert.NotNull(codeIndex);
        Assert.True(codeIndex!.IsUnique);
    }

    [Fact]
    public void AuditLog_ForeignKeys_AreConfigured_ForNullableStaff_AndRequiredAuditType()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new AppDbContext(options);
        var entityType = db.Model.FindEntityType(typeof(AuditLog));
        Assert.NotNull(entityType);

        var staffFk = entityType!.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(AuditLog.StaffPKey));
        Assert.NotNull(staffFk);
        Assert.False(staffFk!.IsRequired);
        Assert.Equal(DeleteBehavior.SetNull, staffFk.DeleteBehavior);

        var auditTypeFk = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(AuditLog.AuditEntryTypeId));
        Assert.NotNull(auditTypeFk);
        Assert.True(auditTypeFk!.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, auditTypeFk.DeleteBehavior);
    }
}
