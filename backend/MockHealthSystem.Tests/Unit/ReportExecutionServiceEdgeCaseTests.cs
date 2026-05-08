using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

/// <summary>
/// Additional ReportExecutionService coverage focused on validation and provider branches that the
/// baseline tests do not exercise.
/// </summary>
public sealed class ReportExecutionServiceEdgeCaseTests
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
    public async Task ExecuteAsync_RejectsWrongPassword_WhenConfiguredPasswordPresent()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_RejectsWrongPassword_WhenConfiguredPasswordPresent));
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "ANY",
            SqlQuery = "SELECT 1 AS \"One\"",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig("the-correct-secret"));

        await Assert.ThrowsAsync<InvalidReportPasswordException>(() =>
            service.ExecuteAsync("totally-wrong", "ANY"));
    }

    [Fact]
    public async Task ExecuteAsync_TrimsPkey_BeforeLookup()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_TrimsPkey_BeforeLookup));
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "TRIMMED",
            SqlQuery = "SELECT 1 AS \"One\"",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());
        var result = await service.ExecuteAsync("soap-secret", "  TRIMMED  ");

        Assert.Single(result.Rows);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsEmptyConfiguredSql()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_RejectsEmptyConfiguredSql));
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "EMPTY",
            SqlQuery = "   ",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());

        var ex = await Assert.ThrowsAsync<ReportQueryValidationException>(() =>
            service.ExecuteAsync("soap-secret", "EMPTY"));

        Assert.Equal("Configured SQL query is empty.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_AcceptsSelect_WithTrailingSemicolon()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_AcceptsSelect_WithTrailingSemicolon));
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "TRAIL_SEMI",
            SqlQuery = "SELECT 1 AS \"One\";",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());
        var result = await service.ExecuteAsync("soap-secret", "TRAIL_SEMI");

        Assert.Single(result.Rows);
        Assert.Equal("1", result.Rows[0][0]);
    }

    [Theory]
    [InlineData("SELECT 1 AS \"One\" /* DROP TABLE Patients */")]
    [InlineData("SELECT 1 AS \"One\" UNION SELECT 1; DELETE FROM Patients")]
    public async Task ExecuteAsync_RejectsSqlContainingDisallowedTokens(string sql)
    {
        await using var db = CreateDb(nameof(ExecuteAsync_RejectsSqlContainingDisallowedTokens) + Guid.NewGuid().ToString("N"));
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "BAD_TOKENS",
            SqlQuery = sql,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());

        var ex = await Assert.ThrowsAsync<ReportQueryValidationException>(() =>
            service.ExecuteAsync("soap-secret", "BAD_TOKENS"));

        // Either the multi-statement detector or the disallowed-token regex catches it.
        Assert.Contains("allowed", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_AuditJoin_ReturnsBlankStaffName_WhenStaffMissing()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_AuditJoin_ReturnsBlankStaffName_WhenStaffMissing));
        db.AuditEntryTypes.Add(new AuditEntryType { Id = 71, Code = "VIEW", DisplayName = "View" });
        db.AuditLogs.Add(new AuditLog
        {
            Id = 81,
            // StaffPKey points to a non-existent staff row to trigger DefaultIfEmpty branch.
            StaffPKey = 9999,
            AuditEntryTypeId = 71,
            CreatedByUser = "system",
            CreatedTimeUtc = DateTime.UtcNow,
            StudyPKey = "S1"
        });
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "AUDIT_NO_STAFF",
            SqlQuery = "SELECT l.\"Id\" AS \"AuditPKey\", t.\"Code\" AS \"AuditTypeCode\", t.\"DisplayName\" AS \"AuditType\", s.\"FirstName\" || ' ' || s.\"LastName\" AS \"StaffName\" FROM \"AuditLogs\" AS l INNER JOIN \"AuditEntryTypes\" AS t ON l.\"AuditEntryTypeId\" = t.\"Id\" LEFT JOIN \"Staff\" AS s ON l.\"StaffPKey\" = s.\"Id\" ORDER BY l.\"CreatedTimeUtc\" DESC",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());
        var result = await service.ExecuteAsync("soap-secret", "AUDIT_NO_STAFF");

        Assert.Single(result.Rows);
        Assert.Equal(string.Empty, result.Rows[0][3]);
    }

    [Fact]
    public async Task ExecuteAsync_InMemoryShortLiteralSelect_ReturnsExpectedSingleColumn()
    {
        await using var db = CreateDb(nameof(ExecuteAsync_InMemoryShortLiteralSelect_ReturnsExpectedSingleColumn));
        db.ReportQueryDefinitions.Add(new ReportQueryDefinition
        {
            PKey = "ONLY_ONE",
            SqlQuery = "SELECT 1 AS \"One\"",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ReportExecutionService(db, CreateConfig());
        var result = await service.ExecuteAsync("soap-secret", "ONLY_ONE");

        Assert.Equal(new[] { "One" }, result.Columns);
        Assert.Single(result.Rows);
        Assert.Equal("1", result.Rows[0][0]);
    }
}
