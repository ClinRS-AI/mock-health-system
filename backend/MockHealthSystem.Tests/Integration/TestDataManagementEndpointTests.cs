using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

[Collection("EnvironmentMutating")]
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
            if (string.Equals(type.Code, "PATIENT_DELETED", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Null(log.PatientPKey);
            }
            else if (type.Code.Contains("PATIENT", StringComparison.OrdinalIgnoreCase))
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

    [Fact]
    public async Task TestDataEndpoints_Return403_WhenAdminKeyMissingOrWrong_IfConfigured()
    {
        using var _ = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "test-admin-key");
        var client = _factory.CreateClient();

        var missingResp = await client.PostAsJsonAsync("/api/v1/test-data/staff/generate", new { count = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, missingResp.StatusCode);

        using var wrongRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/test-data/staff/generate")
        {
            Content = JsonContent.Create(new { count = 1 })
        };
        wrongRequest.Headers.Add("X-Admin-Key", "wrong-key");
        var wrongResp = await client.SendAsync(wrongRequest);
        Assert.Equal(HttpStatusCode.Forbidden, wrongResp.StatusCode);
    }

    [Fact]
    public async Task TestDataEndpoints_Return200_WhenAdminKeyCorrect_IfConfigured()
    {
        using var _ = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "test-admin-key");
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/test-data/staff/generate")
        {
            Content = JsonContent.Create(new { count = 1, seed = 101 })
        };
        request.Headers.Add("X-Admin-Key", "test-admin-key");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSoapReportPkeys_Returns403_WhenAdminKeyConfiguredButMissingHeader()
    {
        using var _ = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "test-admin-key");
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/test-data/soap/report-pkeys");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSoapReportPkeys_ReturnsSortedPkeys_WhenAdminKeyCorrect()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ReportQueryDefinitions.RemoveRange(db.ReportQueryDefinitions);
            var now = DateTime.UtcNow;
            db.ReportQueryDefinitions.AddRange(
                new ReportQueryDefinition
                {
                    PKey = "SOAP_SORT_Z",
                    SqlQuery = "SELECT 1 AS \"X\"",
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                },
                new ReportQueryDefinition
                {
                    PKey = "SOAP_SORT_A",
                    SqlQuery = "SELECT 1 AS \"X\"",
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            await db.SaveChangesAsync();
        }

        using var env = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "test-admin-key");
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test-data/soap/report-pkeys");
        request.Headers.Add("X-Admin-Key", "test-admin-key");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        var pkeys = doc.RootElement.GetProperty("pkeys").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Equal(new[] { "SOAP_SORT_A", "SOAP_SORT_Z" }, pkeys);
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

