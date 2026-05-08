using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class ReportExecutionServiceTests
{
    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfig(string? password = "soap-secret")
    {
        var data = new Dictionary<string, string?>();
        if (password is not null)
        {
            data["SOAP_REPORT_PASSWORD"] = password;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsInvalidPassword_WhenPasswordMissing()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_ThrowsInvalidPassword_WhenPasswordMissing));
        var service = new ReportExecutionService(db, CreateConfig());

        await Assert.ThrowsAsync<InvalidReportPasswordException>(() =>
            service.ExecuteAsync("", "ANY"));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsValidation_WhenPkeyMissing()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_ThrowsValidation_WhenPkeyMissing));
        var service = new ReportExecutionService(db, CreateConfig());

        var ex = await Assert.ThrowsAsync<ReportQueryValidationException>(() =>
            service.ExecuteAsync("soap-secret", " "));

        Assert.Equal("pkey is required.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsValidation_WhenConfiguredPasswordMissing()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_ThrowsValidation_WhenConfiguredPasswordMissing));
        var service = new ReportExecutionService(db, CreateConfig(password: null));

        var ex = await Assert.ThrowsAsync<ReportQueryValidationException>(() =>
            service.ExecuteAsync("soap-secret", "PK"));

        Assert.Equal("SOAP report password is not configured.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsPkeyNotFound_WhenDefinitionMissing()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_ThrowsPkeyNotFound_WhenDefinitionMissing));
        var service = new ReportExecutionService(db, CreateConfig());

        await Assert.ThrowsAsync<ReportPKeyNotFoundException>(() =>
            service.ExecuteAsync("soap-secret", "MISSING"));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsMultiStatementSql()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_RejectsMultiStatementSql));
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "BAD",
            SqlQuery = "SELECT 1; SELECT 2",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());
        var ex = await Assert.ThrowsAsync<ReportQueryValidationException>(() =>
            service.ExecuteAsync("soap-secret", "BAD"));

        Assert.Equal("Only single SELECT statements are allowed.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsNonSelectSql()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_RejectsNonSelectSql));
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "BAD",
            SqlQuery = "UPDATE Patients SET Status = 'Inactive'",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());
        var ex = await Assert.ThrowsAsync<ReportQueryValidationException>(() =>
            service.ExecuteAsync("soap-secret", "BAD"));

        Assert.Equal("Only SELECT queries are allowed.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_InMemoryLiteralSelect_ReturnsExpectedRow()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_InMemoryLiteralSelect_ReturnsExpectedRow));
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "ONE",
            SqlQuery = "SELECT 1 AS \"One\", 'ok' AS \"Message\"",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());
        var result = await service.ExecuteAsync("soap-secret", "ONE");

        Assert.Equal(new[] { "One", "Message" }, result.Columns);
        Assert.Single(result.Rows);
        Assert.Equal(new[] { "1", "ok" }, result.Rows[0]);
    }

    [Fact]
    public async Task ExecuteAsync_InMemoryAuditJoin_ReturnsProjectedAuditData()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_InMemoryAuditJoin_ReturnsProjectedAuditData));
        db.AuditEntryTypes.Add(new AuditEntryType { Id = 11, Code = "LOGIN", DisplayName = "Login" });
        db.Staff.Add(new Staff { Id = 21, FirstName = "Alex", LastName = "Morgan", IsActive = true });
        db.AuditLogs.Add(new AuditLog
        {
            Id = 31,
            StaffPKey = 21,
            AuditEntryTypeId = 11,
            CreatedByUser = "alex",
            CreatedTimeUtc = DateTime.UtcNow,
            StudyPKey = "S1"
        });
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "AUDIT",
            SqlQuery = "SELECT l.\"Id\" AS \"AuditPKey\", t.\"Code\" AS \"AuditTypeCode\", t.\"DisplayName\" AS \"AuditType\", s.\"FirstName\" || ' ' || s.\"LastName\" AS \"StaffName\" FROM \"AuditLogs\" AS l INNER JOIN \"AuditEntryTypes\" AS t ON l.\"AuditEntryTypeId\" = t.\"Id\" LEFT JOIN \"Staff\" AS s ON l.\"StaffPKey\" = s.\"Id\" ORDER BY l.\"CreatedTimeUtc\" DESC",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());
        var result = await service.ExecuteAsync("soap-secret", "AUDIT");

        Assert.Equal(new[] { "AuditPKey", "AuditTypeCode", "AuditType", "StaffName" }, result.Columns);
        Assert.Single(result.Rows);
        Assert.Equal("LOGIN", result.Rows[0][1]);
        Assert.Equal("Alex Morgan", result.Rows[0][3]);
    }

    [Fact]
    public async Task ExecuteAsync_InMemoryUnsupportedSelect_ThrowsValidation()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_InMemoryUnsupportedSelect_ThrowsValidation));
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "UNSUPPORTED",
            SqlQuery = "SELECT 'x' AS \"X\"",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());
        var ex = await Assert.ThrowsAsync<ReportQueryValidationException>(() =>
            service.ExecuteAsync("soap-secret", "UNSUPPORTED"));

        Assert.Equal("The in-memory test provider cannot execute this SQL query.", ex.Message);
    }
}
