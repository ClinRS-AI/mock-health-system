using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class TestDataManagementEndpointTests : IClassFixture<MockHealthSystemWebApplicationFactory>
{
    private readonly MockHealthSystemWebApplicationFactory _factory;

    public TestDataManagementEndpointTests(MockHealthSystemWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateStaff_CreatesRequestedCount()
    {
        await ResetAuditAndStaffTablesAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/test-data/staff/generate", new
        {
            count = 3,
            seed = 123
        });

        response.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(3, db.Staff.Count());
    }

    [Fact]
    public async Task GenerateRecentAuditEvents_CreatesEventsInLastFiveMinutes_WithRequiredReferences()
    {
        await ResetAuditAndStaffTablesAsync();
        await SeedAuditTypesAndPatientAsync();

        var client = _factory.CreateClient();

        var staffResponse = await client.PostAsJsonAsync("/api/v1/test-data/staff/generate", new
        {
            count = 2,
            seed = 42
        });
        staffResponse.EnsureSuccessStatusCode();

        var auditResponse = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 10,
            seed = 99
        });
        auditResponse.EnsureSuccessStatusCode();

        var now = DateTime.UtcNow;
        var fiveMinutesAgo = now.AddMinutes(-5);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditLogs = db.AuditLogs.ToList();
        var auditTypesById = db.AuditEntryTypes.ToDictionary(t => t.Id, t => t);

        Assert.Equal(10, auditLogs.Count);

        foreach (var log in auditLogs)
        {
            Assert.NotNull(log.StaffPKey);
            Assert.InRange(log.CreatedTimeUtc, fiveMinutesAgo, now.AddSeconds(1));

            var type = auditTypesById[log.AuditEntryTypeId];
            if (type.Code.Contains("PATIENT", StringComparison.OrdinalIgnoreCase))
            {
                Assert.NotNull(log.PatientPKey);
            }
        }
    }

    [Fact]
    public async Task GenerateRecentAuditEvents_ReturnsBadRequest_WhenNoStaffExists()
    {
        await ResetAuditAndStaffTablesAsync();
        await SeedAuditTypesAndPatientAsync();

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task ResetAuditAndStaffTablesAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AuditLogs.RemoveRange(db.AuditLogs);
        db.AuditEntryTypes.RemoveRange(db.AuditEntryTypes);
        db.Staff.RemoveRange(db.Staff);
        await db.SaveChangesAsync();
    }

    private async Task SeedAuditTypesAndPatientAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.Patients.Any())
        {
            db.Patients.Add(new Patient
            {
                FirstName = "Pat",
                LastName = "Ient",
                DisplayName = "Ient, Pat",
                Status = "Active",
                PrimaryEmailAddress = "pat.ient@example.com"
            });
        }

        db.AuditEntryTypes.AddRange(
            new AuditEntryType
            {
                Code = "LOGIN",
                DisplayName = "Login"
            },
            new AuditEntryType
            {
                Code = "PATIENT_VIEWED",
                DisplayName = "Patient Viewed"
            });

        await db.SaveChangesAsync();
    }
}

