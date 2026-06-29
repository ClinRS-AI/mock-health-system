using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Integration tests for /api/v1/patients endpoints.
/// Auth mode is set to None so requests are permitted without credentials.
/// </summary>
public sealed class PatientEndpointTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public PatientEndpointTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- GET /patients/{id} ----

    [Fact]
    public async Task GetPatient_Returns404_WhenPatientDoesNotExist()
    {
        await EnsureNoneModeAsync();
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/patients/999999");

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPatient_Returns200WithPatientData_WhenPatientExists()
    {
        await EnsureNoneModeAsync();
        var patientId = await SeedPatientAsync("GetById", "Patient");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/patients/{patientId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal(patientId, body!.Id);
        Assert.Equal("GetById", body.FirstName);
        Assert.Equal("Patient", body.LastName);
    }

    // ---- POST /patients ----

    [Fact]
    public async Task CreatePatient_Returns201WithCreatedPatient()
    {
        await EnsureNoneModeAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Created",
            lastName = "ViaApi",
            status = "Active"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.True(body!.Id > 0);
        Assert.Equal("Created", body.FirstName);
        Assert.Equal("ViaApi", body.LastName);
    }

    [Fact]
    public async Task CreatePatient_SetsLocationHeader()
    {
        await EnsureNoneModeAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Location",
            lastName = "Test"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.NotNull(resp.Headers.Location);
    }

    [Fact]
    public async Task CreatePatient_WithPhone_PersistsPhone()
    {
        await EnsureNoneModeAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Phone",
            lastName = "Test",
            phone1 = new { number = "555-123-4567" }
        });

        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(body!.Phone1);
        Assert.Equal("555-123-4567", body.Phone1!.Number);
    }

    // ---- PUT /patients/{id} ----

    [Fact]
    public async Task UpdatePatient_Returns200WithUpdatedData()
    {
        await EnsureNoneModeAsync();
        var patientId = await SeedPatientAsync("OriginalFirst", "OriginalLast");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/patients/{patientId}", new
        {
            firstName = "UpdatedFirst",
            lastName = "UpdatedLast",
            city = "NewCity",
            country = "US"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal("UpdatedFirst", body!.FirstName);
        Assert.Equal("UpdatedLast", body.LastName);
        Assert.Equal("NewCity", body.City);
    }

    [Fact]
    public async Task UpdatePatient_Returns404_WhenPatientDoesNotExist()
    {
        await EnsureNoneModeAsync();
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync("/api/v1/patients/999998", new
        {
            firstName = "Ghost",
            lastName = "Patient"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // ---- PATCH /patients/{id} ----

    [Fact]
    public async Task PatchPatient_Returns200_AndUpdatesOnlyProvidedFields()
    {
        await EnsureNoneModeAsync();
        var patientId = await SeedPatientAsync("PatchFirst", "PatchLast", genderCode: "M");
        var client = _factory.CreateClient();

        var resp = await client.PatchAsJsonAsync($"/api/v1/patients/{patientId}", new
        {
            city = "PatchedCity"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal("PatchedCity", body!.City);
        Assert.Equal("M", body.GenderCode); // unchanged
        Assert.Equal("PatchFirst", body.FirstName); // unchanged
    }

    [Fact]
    public async Task PatchPatient_Returns404_WhenPatientDoesNotExist()
    {
        await EnsureNoneModeAsync();
        var client = _factory.CreateClient();

        var resp = await client.PatchAsJsonAsync("/api/v1/patients/999997", new { city = "Nowhere" });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchPatient_WithPhone1_UpdatesSlot1AndLeavesSlot2Untouched()
    {
        await EnsureNoneModeAsync();
        var patientId = await SeedPatientWithPhonesAsync("PhonePatch", "Test", phone1: "585-489-9509", phone2: "555-000-1111");
        var client = _factory.CreateClient();

        var resp = await client.PatchAsJsonAsync($"/api/v1/patients/{patientId}", new
        {
            phone1 = new { number = "585-489-1111" }
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal("585-489-1111", body!.Phone1!.Number);
        Assert.Equal("555-000-1111", body.Phone2!.Number);
    }

    [Fact]
    public async Task PatchPatient_WithNoPhoneFields_LeavesAllPhoneSlotsUntouched()
    {
        await EnsureNoneModeAsync();
        var patientId = await SeedPatientWithPhonesAsync("PhoneUntouched", "Test", phone1: "585-489-9509", phone2: "555-000-1111");
        var client = _factory.CreateClient();

        var resp = await client.PatchAsJsonAsync($"/api/v1/patients/{patientId}", new
        {
            city = "PatchedCity"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal("585-489-9509", body!.Phone1!.Number);
        Assert.Equal("555-000-1111", body.Phone2!.Number);
    }

    // ---- DELETE /patients/{id} ----

    [Fact]
    public async Task DeletePatient_Returns204_WhenPatientExists()
    {
        await EnsureNoneModeAsync();
        var patientId = await SeedPatientAsync("ToDelete", "Patient");
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/patients/{patientId}");

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task DeletePatient_Returns404_WhenPatientDoesNotExist()
    {
        await EnsureNoneModeAsync();
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync("/api/v1/patients/999996");

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePatient_MakesPatientUnretrievable()
    {
        await EnsureNoneModeAsync();
        var patientId = await SeedPatientAsync("DeletedPatient", "Gone");
        var client = _factory.CreateClient();

        await client.DeleteAsync($"/api/v1/patients/{patientId}");
        var getResp = await client.GetAsync($"/api/v1/patients/{patientId}");

        await ApiErrorAssertions.AssertApiErrorAsync(getResp, HttpStatusCode.NotFound);
    }

    // ---- POST /patients/search ----

    [Fact]
    public async Task SearchPatients_FiltersByFirstName()
    {
        await EnsureNoneModeAsync();
        var unique = $"SearchFirst{Guid.NewGuid():N}"[..20];
        await SeedPatientAsync(unique, "SearchLast");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/patients/search", new
        {
            firstName = unique
        });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientDto[]>();
        Assert.NotNull(items);
        Assert.Contains(items!, p => p.FirstName == unique);
    }

    [Fact]
    public async Task SearchPatients_FiltersByStatus()
    {
        await EnsureNoneModeAsync();
        var uniqueLast = $"StatusSearch{Guid.NewGuid():N}"[..20];
        await SeedPatientAsync("StatusFirst", uniqueLast, status: "Inactive");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/patients/search", new
        {
            status = "Inactive",
            lastName = uniqueLast
        });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientDto[]>();
        Assert.NotNull(items);
        Assert.All(items!, p => Assert.Equal("Inactive", p.Status));
    }

    [Fact]
    public async Task SearchPatients_ReturnsEmpty_WhenNoCriteriaMatch()
    {
        await EnsureNoneModeAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/patients/search", new
        {
            firstName = "ZZZNobodyHasThisFirstName" + Guid.NewGuid().ToString("N")
        });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientDto[]>();
        Assert.NotNull(items);
        Assert.Empty(items!);
    }

    // ---- PUT /patients/{id}/status ----

    [Fact]
    public async Task SetStatus_Updates_StatusAndReason()
    {
        await EnsureNoneModeAsync();
        var patientId = await SeedPatientAsync("StatusUpdate", "Test");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/patients/{patientId}/status", new
        {
            status = "Inactive",
            reason = "Withdrew from study"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal("Inactive", body!.Status);
        Assert.Equal("Withdrew from study", body.StatusReason);
    }

    [Fact]
    public async Task SetStatus_Returns404_WhenPatientDoesNotExist()
    {
        await EnsureNoneModeAsync();
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync("/api/v1/patients/999995/status", new
        {
            status = "Inactive",
            reason = "Test"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // ---- GET /patients/odata ----

    [Fact]
    public async Task GetPatientsOdata_Returns200WithList()
    {
        await EnsureNoneModeAsync();
        await SeedPatientAsync("OdataTest", "Patient");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/patients/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await resp.Content.ReadFromJsonAsync<PatientDto[]>();
        Assert.NotNull(items);
    }

    // ---- Helpers ----

    private Task EnsureNoneModeAsync() =>
        IntegrationAuthSettingsHelper.EnsureNoneModeAsync(_factory.Services);

    private async Task<int> SeedPatientAsync(
        string firstName, string lastName,
        string? genderCode = null, string status = "Active")
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var patient = new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            Status = status,
            GenderCode = genderCode,
            Uid = Guid.NewGuid()
        };

        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return patient.Id;
    }

    private async Task<int> SeedPatientWithPhonesAsync(
        string firstName, string lastName, string? phone1 = null, string? phone2 = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var patient = new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            Status = "Active",
            Uid = Guid.NewGuid()
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        if (phone1 != null)
            db.PatientPhones.Add(new PatientPhone { PatientId = patient.Id, Slot = 1, Number = phone1, RawNumber = new string(phone1.Where(char.IsDigit).ToArray()) });
        if (phone2 != null)
            db.PatientPhones.Add(new PatientPhone { PatientId = patient.Id, Slot = 2, Number = phone2, RawNumber = new string(phone2.Where(char.IsDigit).ToArray()) });
        await db.SaveChangesAsync();

        return patient.Id;
    }

    // Minimal DTO for deserializing responses.
    private sealed class PatientDto
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Status { get; set; }
        public string? StatusReason { get; set; }
        public string? GenderCode { get; set; }
        public string? City { get; set; }
        public PhoneDto? Phone1 { get; set; }
        public PhoneDto? Phone2 { get; set; }
    }

    private sealed class PhoneDto
    {
        public string? Number { get; set; }
        public string? RawNumber { get; set; }
        public bool OutOfService { get; set; }
    }
}
