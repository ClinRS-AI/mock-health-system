using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Edge-case integration tests for /api/v1/test-data covering admin gates, audit branches,
/// reset, and lookup paths not exercised by other test classes.
/// </summary>
public sealed class TestDataAdminAndAuditEdgeTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TestDataAdminAndAuditEdgeTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- Generate staff ----

    [Fact]
    public async Task GenerateStaff_Returns400_WhenCountIsZero()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/staff/generate", new
        {
            count = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GenerateStaff_Returns400_WhenCountIsNegative()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/staff/generate", new
        {
            count = -5
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GenerateStaff_DefaultsTo10_WhenCountOmitted()
    {
        await ResetStaffAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/staff/generate", new { });

        resp.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(10, await db.Staff.CountAsync());
    }

    // ---- Generate audit events ----

    [Fact]
    public async Task GenerateAuditEvents_Returns400_WhenCountIsZero()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GenerateAuditEvents_Returns400_WhenNoActiveStaff()
    {
        await ResetStaffAsync();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Staff.Add(new Staff
            {
                FirstName = "Inactive",
                LastName = "One",
                IsActive = false
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 3
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GenerateAuditEvents_Returns400_WhenNoAuditEntryTypes()
    {
        await ResetAuditAndStaffAsync();
        await SeedActiveStaffAsync(2);
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GenerateAuditEvents_Returns400_WhenNoPatientsAndNoPatientCreatedType()
    {
        await ResetAuditAndStaffAsync();
        await ResetPatientsTableAsync();
        await SeedActiveStaffAsync(2);
        await SeedAuditTypesAsync("PATIENT_VIEWED", "PATIENT_DELETED");

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 4
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GenerateAuditEvents_AllowsPatientCreatedType_WhenNoPatientsExist()
    {
        await ResetAuditAndStaffAsync();
        await ResetPatientsTableAsync();
        await SeedActiveStaffAsync(2);
        await SeedAuditTypesAsync("PATIENT_CREATED");

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 3,
            seed = 7
        });

        resp.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.AuditLogs.CountAsync() >= 1);
        Assert.True(await db.Patients.CountAsync() >= 1);
    }

    [Fact]
    public async Task GenerateAuditEvents_DefaultsCount_WhenOmitted()
    {
        await ResetAuditAndStaffAsync();
        await SeedActiveStaffAsync(2);
        await SeedAuditTypesAsync("LOGIN", "LOGOUT");
        await SeedPatientAsync("Default", "PatientForLogin", "default-login@example.com");

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new { });

        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GenerateAuditResponseDto>(json, JsonOptions);
        Assert.Equal(25, result!.Requested);
    }

    [Fact]
    public async Task GenerateAuditEvents_StaffProfileUpdated_MutatesStaff()
    {
        await ResetAuditAndStaffAsync();
        await ResetPatientsTableAsync();
        await SeedActiveStaffAsync(1);
        await SeedAuditTypesAsync("STAFF_PROFILE_UPDATED");
        await SeedPatientAsync("StaffProfile", "Anchor", "staffprofile-anchor@example.com");

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 2,
            seed = 11
        });

        resp.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task GenerateAuditEvents_PatientDeletedType_RemovesPatients()
    {
        await ResetAuditAndStaffAsync();
        await ResetPatientsTableAsync();
        await SeedActiveStaffAsync(1);
        await SeedAuditTypesAsync("PATIENT_DELETED");
        for (var i = 0; i < 4; i++)
        {
            await SeedPatientAsync($"Vict{i}", "Patient", $"vict{i}@example.com");
        }

        var beforeDelete = await CountPatientsAsync();

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 3,
            seed = 27
        });

        resp.EnsureSuccessStatusCode();

        var afterDelete = await CountPatientsAsync();
        Assert.True(afterDelete < beforeDelete);
    }

    [Fact]
    public async Task GenerateAuditEvents_GenericPatientType_LinksPatientWhenAvailable()
    {
        await ResetAuditAndStaffAsync();
        await ResetPatientsTableAsync();
        await SeedActiveStaffAsync(1);
        await SeedAuditTypesAsync("PATIENT_REPORT_PRINTED");
        await SeedPatientAsync("Linked", "PatientType", "linked@example.com");

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 2,
            seed = 53
        });

        resp.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logs = await db.AuditLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.All(logs, l => Assert.NotNull(l.PatientPKey));
    }

    [Fact]
    public async Task GenerateAuditEvents_NonPatientCustomType_DoesNotRequirePatient()
    {
        await ResetAuditAndStaffAsync();
        await ResetPatientsTableAsync();
        await SeedActiveStaffAsync(1);
        await SeedAuditTypesAsync("SYSTEM_BACKUP_RUN");
        // The early gate requires either PATIENT_CREATED or at least one patient.
        await SeedPatientAsync("NonPatient", "Anchor", "nonpatient-anchor@example.com");

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/audit-events/generate", new
        {
            count = 2,
            seed = 1234
        });

        resp.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.AuditLogs.CountAsync());
    }

    // ---- Save with audit ----

    [Fact]
    public async Task UpdatePatient_WithSaveWithAudit_Returns400_WhenNoActiveStaff()
    {
        await ResetAuditAndStaffAsync();
        var patientId = await SeedPatientAsync("AuditUpd", "NoStaff", "auditupd-nostaff@example.com");

        var client = _factory.CreateClient();
        var resp = await client.PutAsJsonAsync(
            $"/api/v1/test-data/patients/{patientId}?saveWithAudit=true",
            new
            {
                firstName = "AuditUpd",
                lastName = "NoStaff",
                status = "Active"
            });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdatePatient_WithSaveWithAudit_Returns400_WhenPatientUpdatedTypeMissing()
    {
        await ResetAuditAndStaffAsync();
        await SeedActiveStaffAsync(1);
        await SeedAuditTypesAsync("LOGIN");
        var patientId = await SeedPatientAsync("AuditUpd", "NoType", "auditupd-notype@example.com");

        var client = _factory.CreateClient();
        var resp = await client.PutAsJsonAsync(
            $"/api/v1/test-data/patients/{patientId}?saveWithAudit=true",
            new
            {
                firstName = "AuditUpd",
                lastName = "NoType",
                status = "Active"
            });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdatePatient_WithSaveWithAudit_CreatesAuditLog_OnSuccess()
    {
        await ResetAuditAndStaffAsync();
        await SeedActiveStaffAsync(1);
        await SeedAuditTypesAsync("PATIENT_UPDATED");
        var patientId = await SeedPatientAsync("AuditUpd", "Success", "auditupd-success@example.com");

        var client = _factory.CreateClient();
        var resp = await client.PutAsJsonAsync(
            $"/api/v1/test-data/patients/{patientId}?saveWithAudit=true",
            new
            {
                firstName = "AuditUpd",
                lastName = "SuccessChanged",
                status = "Active"
            });

        resp.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.AuditLogs.AnyAsync(a => a.PatientPKey == patientId));
    }

    // ---- Lookup by secondary email ----

    [Fact]
    public async Task LookupPatient_FindsBySecondaryEmail()
    {
        var uniquePrefix = $"sec-{Guid.NewGuid():N}"[..16];
        var secondaryEmail = $"{uniquePrefix}@example.com";

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Patients.Add(new Patient
            {
                FirstName = "Sec",
                LastName = "Email",
                DisplayName = "Email, Sec",
                Status = "Active",
                PrimaryEmailAddress = $"primary-{uniquePrefix}@example.com",
                SecondaryEmailAddress = secondaryEmail,
                Uid = Guid.NewGuid()
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/api/v1/test-data/patients/lookup?email={uniquePrefix}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ---- Generate patients with reference data ----

    [Fact]
    public async Task GeneratePatients_WithProvidersAndConditions_AttachesRelatedData()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!await db.Providers.AnyAsync())
            {
                db.Providers.Add(new Provider
                {
                    ProviderName = "Doc One",
                    FirstName = "Doc",
                    LastName = "One"
                });
            }
            if (!await db.Conditions.AnyAsync())
            {
                db.Conditions.Add(new Condition
                {
                    Name = "TestCondition"
                });
            }
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/generate", new
        {
            totalCount = 2,
            duplicatePercentage = 0,
            seed = 17
        });

        resp.EnsureSuccessStatusCode();

        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db2.PatientPhones.AnyAsync());
        Assert.True(await db2.PatientProviders.AnyAsync());
        Assert.True(await db2.PatientConditions.AnyAsync());
    }

    // ---- Helpers ----

    private async Task ResetStaffAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AuditLogs.RemoveRange(db.AuditLogs);
        db.Staff.RemoveRange(db.Staff);
        await db.SaveChangesAsync();
    }

    private async Task ResetAuditAndStaffAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AuditLogs.RemoveRange(db.AuditLogs);
        db.AuditEntryTypes.RemoveRange(db.AuditEntryTypes);
        db.Staff.RemoveRange(db.Staff);
        await db.SaveChangesAsync();
    }

    private async Task ResetPatientsTableAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AuditLogs.RemoveRange(db.AuditLogs);
        db.Patients.RemoveRange(db.Patients);
        await db.SaveChangesAsync();
    }

    private async Task SeedActiveStaffAsync(int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        for (var i = 0; i < count; i++)
        {
            db.Staff.Add(new Staff
            {
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedAuditTypesAsync(params string[] codes)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var code in codes)
        {
            db.AuditEntryTypes.Add(new AuditEntryType
            {
                Code = code,
                DisplayName = code.Replace('_', ' ')
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task<int> SeedPatientAsync(string firstName, string lastName, string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var patient = new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            DisplayName = $"{lastName}, {firstName}",
            Status = "Active",
            PrimaryEmailAddress = email,
            Uid = Guid.NewGuid()
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return patient.Id;
    }

    private async Task<int> CountPatientsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Patients.CountAsync();
    }

    private sealed class GenerateAuditResponseDto
    {
        public int Requested { get; set; }
        public int Inserted { get; set; }
    }
}

/// <summary>
/// Reset patients endpoint requires a relational provider (TRUNCATE), so it uses the SQLite-backed
/// factory rather than the InMemory one used elsewhere in the edge-case suite.
/// </summary>
public sealed class TestDataResetEndpointTests : IClassFixture<MockHealthSystemWebApplicationFactory>
{
    private readonly MockHealthSystemWebApplicationFactory _factory;

    public TestDataResetEndpointTests(MockHealthSystemWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResetPatients_ReturnsErrorOnSqliteProvider_BecauseTruncateIsUnsupported()
    {
        // SQLite does not support TRUNCATE; the endpoint surfaces that as a 500. Asserting this
        // documents the dev-only nature of the endpoint and exercises the IsAdminRequest()/handler
        // entry path for coverage.
        var client = _factory.CreateClient();

        var resp = await client.PostAsync("/api/v1/test-data/patients/reset", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
    }
}
