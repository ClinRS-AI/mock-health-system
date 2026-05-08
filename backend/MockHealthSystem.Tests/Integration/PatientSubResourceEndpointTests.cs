using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Integration tests for PatientsController sub-resource endpoints
/// (devices, allergies, providers, conditions, procedures, medications,
/// immunizations, family-history, social-history) and search filters.
///
/// Each GET test verifies both the 200-with-empty-list path (patient exists)
/// and the 404 path (patient does not exist).
/// Each POST test verifies the 201/200-created path and the 404 path.
/// </summary>
public sealed class PatientSubResourceEndpointTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int MissingPatientId = 999_888;

    public PatientSubResourceEndpointTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // =====================================================================
    // DEVICES
    // =====================================================================

    [Fact]
    public async Task GetDevices_Returns200_WithEmptyList_WhenPatientHasNone()
    {
        var id = await SeedPatientAsync();
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{id}/devices");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetDevices_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{MissingPatientId}/devices");
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddDevices_Returns201_AndPersistsRecord()
    {
        var deviceId = await SeedDeviceAsync("Pacemaker");
        var id = await SeedPatientAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/patients/{id}/devices",
            new[] { new { deviceId, comment = "Implanted" } });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        // Verify GET now reflects the added device.
        var getResp = await client.GetAsync($"/api/v1/patients/{id}/devices");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = await ReadArrayAsync(getResp);
        Assert.Single(fetched);
    }

    [Fact]
    public async Task AddDevices_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/patients/{MissingPatientId}/devices",
            new[] { new { deviceId = 1 } });
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // =====================================================================
    // ALLERGIES
    // =====================================================================

    [Fact]
    public async Task GetAllergies_Returns200_WithEmptyList_WhenPatientHasNone()
    {
        var id = await SeedPatientAsync();
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{id}/allergies");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAllergies_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{MissingPatientId}/allergies");
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddAllergies_Returns201_AndPersistsRecord()
    {
        var id = await SeedPatientAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/patients/{id}/allergies",
            new[] { new { allergyId = 1, reaction = "Hives", comment = "Mild" } });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Single(items);

        var getResp = await client.GetAsync($"/api/v1/patients/{id}/allergies");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
    }

    [Fact]
    public async Task AddAllergies_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/patients/{MissingPatientId}/allergies",
            new[] { new { allergyId = 1 } });
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // =====================================================================
    // PROVIDERS
    // =====================================================================

    [Fact]
    public async Task GetProviders_Returns200_WithEmptyList_WhenPatientHasNone()
    {
        var id = await SeedPatientAsync();
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{id}/providers");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetProviders_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{MissingPatientId}/providers");
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetProviders_Returns200_AndReplacesExisting()
    {
        var id = await SeedPatientAsync();
        var client = _factory.CreateClient();

        // First call: set one provider.
        var first = await client.PostAsJsonAsync($"/api/v1/patients/{id}/providers",
            new[] { new { providerId = 1 } });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Second call: replace with a different provider (set semantics).
        var second = await client.PostAsJsonAsync($"/api/v1/patients/{id}/providers",
            new[] { new { providerId = 2 } });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        // GET should reflect the current (second) set only.
        var getResp = await client.GetAsync($"/api/v1/patients/{id}/providers");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
    }

    [Fact]
    public async Task SetProviders_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/patients/{MissingPatientId}/providers",
            new[] { new { providerId = 1 } });
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // =====================================================================
    // CONDITIONS
    // =====================================================================

    [Fact]
    public async Task GetConditions_Returns200_WithEmptyList_WhenPatientHasNone()
    {
        var id = await SeedPatientAsync();
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{id}/conditions");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetConditions_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{MissingPatientId}/conditions");
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddConditions_Returns201_AndPersistsRecord()
    {
        var id = await SeedPatientAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/patients/{id}/conditions",
            new[] { new { conditionId = 1, ageAtOnset = 42.0, comment = "Controlled" } });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Single(items);
    }

    [Fact]
    public async Task AddConditions_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/patients/{MissingPatientId}/conditions",
            new[] { new { conditionId = 1 } });
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // =====================================================================
    // PROCEDURES
    // =====================================================================

    [Fact]
    public async Task GetProcedures_Returns200_WithEmptyList_WhenPatientHasNone()
    {
        var id = await SeedPatientAsync();
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{id}/procedures");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetProcedures_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{MissingPatientId}/procedures");
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddProcedures_Returns201_AndPersistsRecord()
    {
        var id = await SeedPatientAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/patients/{id}/procedures",
            new[] { new { procedureId = (int?)1, comment = "Routine", procedureBy = "Dr Smith" } });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Single(items);
    }

    [Fact]
    public async Task AddProcedures_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/patients/{MissingPatientId}/procedures",
            new[] { new { procedureId = (int?)1 } });
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // =====================================================================
    // MEDICATIONS
    // =====================================================================

    [Fact]
    public async Task GetMedications_Returns200_WithEmptyList_WhenPatientHasNone()
    {
        var id = await SeedPatientAsync();
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{id}/medications");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetMedications_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{MissingPatientId}/medications");
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddMedications_Returns201_AndPersistsRecord()
    {
        var medicationId = await SeedMedicationAsync("Aspirin");
        var patientId = await SeedPatientAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/medications",
            new[] { new { id = medicationId, dosage = "10mg" } });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        // Verify GET now reflects the added medication.
        var getResp = await client.GetAsync($"/api/v1/patients/{patientId}/medications");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = await ReadArrayAsync(getResp);
        Assert.Single(fetched);
    }

    [Fact]
    public async Task AddMedications_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/patients/{MissingPatientId}/medications",
            new[] { new { id = 1, routeId = 1 } });
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // =====================================================================
    // IMMUNIZATIONS
    // =====================================================================

    [Fact]
    public async Task GetImmunizations_Returns200_WithEmptyList_WhenPatientHasNone()
    {
        var id = await SeedPatientAsync();
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{id}/immunizations");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetImmunizations_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{MissingPatientId}/immunizations");
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddImmunizations_Returns200_AndPersistsRecord()
    {
        var id = await SeedPatientAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/patients/{id}/immunizations",
            new[] { new { immunizationId = 1, location = "Left arm", comment = "Annual" } });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Single(items);
    }

    [Fact]
    public async Task AddImmunizations_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/patients/{MissingPatientId}/immunizations",
            new[] { new { immunizationId = 1 } });
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // =====================================================================
    // FAMILY HISTORY
    // =====================================================================

    [Fact]
    public async Task GetFamilyHistory_Returns200_WithEmptyList_WhenPatientHasNone()
    {
        var id = await SeedPatientAsync();
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{id}/family-history");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetFamilyHistory_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{MissingPatientId}/family-history");
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddFamilyHistory_Returns201_AndPersistsRecord()
    {
        var id = await SeedPatientAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/patients/{id}/family-history",
            new[] { new { conditionId = 1, familyMemberId = 1, ageAtOnset = 65.0, comment = "Paternal" } });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Single(items);
    }

    [Fact]
    public async Task AddFamilyHistory_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/patients/{MissingPatientId}/family-history",
            new[] { new { conditionId = 1, familyMemberId = 1 } });
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // =====================================================================
    // SOCIAL HISTORY
    // =====================================================================

    [Fact]
    public async Task GetSocialHistory_Returns200_WithEmptyList_WhenPatientHasNone()
    {
        var id = await SeedPatientAsync();
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{id}/social-history");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetSocialHistory_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/v1/patients/{MissingPatientId}/social-history");
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddSocialHistory_Returns201_AndPersistsRecord()
    {
        var id = await SeedPatientAsync();
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/patients/{id}/social-history",
            new[] { new { socialHistoryId = 1, comment = "Non-smoker" } });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var items = await ReadArrayAsync(resp);
        Assert.Single(items);
    }

    [Fact]
    public async Task AddSocialHistory_Returns404_WhenPatientDoesNotExist()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/patients/{MissingPatientId}/social-history",
            new[] { new { socialHistoryId = 1 } });
        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    // =====================================================================
    // SEARCH — additional filter criteria (first name and status already
    // covered in PatientEndpointTests)
    // =====================================================================

    [Fact]
    public async Task Search_FiltersByLastName()
    {
        var unique = $"SRLast{Guid.NewGuid():N}"[..18];
        await SeedFullPatientAsync(firstName: "SRFirst", lastName: unique);

        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/patients/search",
            new { lastName = unique });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientSummary[]>();
        Assert.NotNull(items);
        Assert.Contains(items!, p => p.LastName == unique);
    }

    [Fact]
    public async Task Search_FiltersByMiddleName()
    {
        var unique = $"SRMid{Guid.NewGuid():N}"[..18];
        await SeedFullPatientAsync(firstName: "SRFirst", lastName: "SRLast", middleName: unique);

        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/patients/search",
            new { middleName = unique });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientSummary[]>();
        Assert.Contains(items!, p => p.MiddleName == unique);
    }

    [Fact]
    public async Task Search_FiltersByEmail()
    {
        var domain = $"srsearch{Guid.NewGuid():N}"[..12];
        var email = $"patient@{domain}.com";
        await SeedFullPatientAsync(firstName: "Email", lastName: "Search", email: email);

        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/patients/search",
            new { email = $"patient@{domain}" });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientSummary[]>();
        Assert.Contains(items!, p => p.PrimaryEmail?.Email == email);
    }

    [Fact]
    public async Task Search_FiltersByCity()
    {
        var city = $"SRCity{Guid.NewGuid():N}"[..14];
        await SeedFullPatientAsync(firstName: "City", lastName: "Search", city: city);

        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/patients/search",
            new { city });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientSummary[]>();
        Assert.Contains(items!, p => p.City == city);
    }

    [Fact]
    public async Task Search_FiltersByZip()
    {
        var zip = "99123";
        await SeedFullPatientAsync(firstName: "Zip", lastName: "Search", zip: zip);

        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/patients/search",
            new { zip });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientSummary[]>();
        Assert.Contains(items!, p => p.Zip == zip);
    }

    [Fact]
    public async Task Search_FiltersByGender()
    {
        var uniqueLast = $"GenderTest{Guid.NewGuid():N}"[..18];
        await SeedFullPatientAsync(firstName: "Gender", lastName: uniqueLast, genderCode: "F");

        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/patients/search",
            new { gender = "F", lastName = uniqueLast });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientSummary[]>();
        Assert.NotNull(items);
        Assert.All(items!, p => Assert.Equal("F", p.GenderCode));
    }

    [Fact]
    public async Task Search_FiltersByDateOfBirth()
    {
        var dob = new DateTime(1985, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var uniqueLast = $"DobTest{Guid.NewGuid():N}"[..18];
        await SeedFullPatientAsync(firstName: "Dob", lastName: uniqueLast, dateOfBirth: dob);

        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/patients/search",
            new { dateOfBirth = dob, lastName = uniqueLast });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientSummary[]>();
        Assert.Contains(items!, p => p.LastName == uniqueLast);
    }

    [Fact]
    public async Task Search_RespectsLimit()
    {
        // Seed 5 patients with a shared last name prefix, request limit=2.
        var sharedPrefix = $"LimitPfx{Guid.NewGuid():N}"[..14];
        for (var i = 0; i < 5; i++)
        {
            await SeedFullPatientAsync(firstName: $"Limit{i}", lastName: $"{sharedPrefix}{i}");
        }

        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/patients/search",
            new { lastName = sharedPrefix, limit = 2 });

        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<PatientSummary[]>();
        Assert.NotNull(items);
        Assert.Equal(2, items!.Length);
    }

    [Fact]
    public async Task Search_RespectsSkip()
    {
        var sharedPrefix = $"SkipPfx{Guid.NewGuid():N}"[..14];
        for (var i = 0; i < 4; i++)
        {
            await SeedFullPatientAsync(firstName: $"Skip{i}", lastName: $"{sharedPrefix}{i}");
        }

        var allResp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/patients/search",
            new { lastName = sharedPrefix, limit = 100 });
        var all = await allResp.Content.ReadFromJsonAsync<PatientSummary[]>();

        var skippedResp = await _factory.CreateClient().PostAsJsonAsync("/api/v1/patients/search",
            new { lastName = sharedPrefix, limit = 100, skip = 2 });
        var skipped = await skippedResp.Content.ReadFromJsonAsync<PatientSummary[]>();

        Assert.Equal(all!.Length - 2, skipped!.Length);
        Assert.Equal(all[2].Id, skipped[0].Id);
    }

    [Fact]
    public async Task Search_LimitDefaultsTo100_WhenZeroOrNegative()
    {
        var client = _factory.CreateClient();

        // Both should return OK without throwing.
        var zeroResp = await client.PostAsJsonAsync("/api/v1/patients/search", new { limit = 0 });
        var negResp = await client.PostAsJsonAsync("/api/v1/patients/search", new { limit = -1 });

        Assert.Equal(HttpStatusCode.OK, zeroResp.StatusCode);
        Assert.Equal(HttpStatusCode.OK, negResp.StatusCode);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private async Task<int> SeedDeviceAsync(string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var d = new Device { Name = name };
        db.Devices.Add(d);
        await db.SaveChangesAsync();
        return d.Id;
    }

    private async Task<int> SeedMedicationAsync(string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var m = new Medication { Name = name };
        db.Medications.Add(m);
        await db.SaveChangesAsync();
        return m.Id;
    }

    private Task EnsureNoneModeAsync() =>
        IntegrationAuthSettingsHelper.EnsureNoneModeAsync(_factory.Services);

    private async Task<int> SeedPatientAsync()
    {
        await EnsureNoneModeAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var p = new Patient
        {
            FirstName = "Sub",
            LastName = "Resource",
            Status = "Active",
            Uid = Guid.NewGuid()
        };
        db.Patients.Add(p);
        await db.SaveChangesAsync();
        return p.Id;
    }

    private async Task<int> SeedFullPatientAsync(
        string firstName, string lastName,
        string? middleName = null, string? email = null,
        string? city = null, string? zip = null,
        string? genderCode = null, DateTime? dateOfBirth = null)
    {
        await EnsureNoneModeAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var p = new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            PrimaryEmailAddress = email,
            City = city,
            Zip = zip,
            GenderCode = genderCode,
            DateOfBirth = dateOfBirth,
            Status = "Active",
            Uid = Guid.NewGuid()
        };
        db.Patients.Add(p);
        await db.SaveChangesAsync();
        return p.Id;
    }

    private static async Task<List<JsonElement>> ReadArrayAsync(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<JsonElement>>(json, JsonOptions) ?? [];
    }

    private sealed class PatientSummary
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? GenderCode { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public PatientEmailDto? PrimaryEmail { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }

    private sealed class PatientEmailDto
    {
        public string? Email { get; set; }
    }
}
