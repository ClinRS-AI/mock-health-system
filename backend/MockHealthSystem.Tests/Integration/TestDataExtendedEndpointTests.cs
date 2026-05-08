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
/// Extended integration tests for /api/v1/test-data endpoints not covered by TestDataManagementEndpointTests.
/// </summary>
public sealed class TestDataExtendedEndpointTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TestDataExtendedEndpointTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- POST /test-data/patients/add ----

    [Fact]
    public async Task AddTestPatient_Returns200_WithIdAndUid_WhenRequestIsValid()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/add", new
        {
            firstName = "Alice",
            lastName = "Smith",
            email = "alice.smith@example.com"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<AddPatientResult>();
        Assert.True(body!.Id > 0);
        Assert.NotEqual(Guid.Empty, body.Uid);
    }

    [Fact]
    public async Task AddTestPatient_PersistedPatient_IsRetrievableByLookup()
    {
        var client = _factory.CreateClient();
        var uniqueEmail = $"add-lookup-{Guid.NewGuid():N}@example.com";

        var addResp = await client.PostAsJsonAsync("/api/v1/test-data/patients/add", new
        {
            firstName = "Bob",
            lastName = "Jones",
            email = uniqueEmail
        });
        addResp.EnsureSuccessStatusCode();
        var addResult = await addResp.Content.ReadFromJsonAsync<AddPatientResult>();

        var lookupResp = await client.GetAsync($"/api/v1/test-data/patients/lookup?id={addResult!.Id}");

        Assert.Equal(HttpStatusCode.OK, lookupResp.StatusCode);
        var patient = await lookupResp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal("Bob", patient!.FirstName);
        Assert.Equal("Jones", patient.LastName);
    }

    [Fact]
    public async Task AddTestPatient_Returns400_WhenFirstNameIsMissing()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/add", new
        {
            lastName = "Smith",
            email = "test@example.com"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddTestPatient_Returns400_WhenLastNameIsMissing()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/add", new
        {
            firstName = "Alice",
            email = "test@example.com"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddTestPatient_Returns400_WhenEmailIsMissing()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/add", new
        {
            firstName = "Alice",
            lastName = "Smith"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    // ---- GET /test-data/patients/lookup ----

    [Fact]
    public async Task LookupPatient_Returns400_WhenNoParametersProvided()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/patients/lookup");

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LookupPatient_Returns404_WhenPatientNotFoundById()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/patients/lookup?id=999999");

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LookupPatient_Returns200_WhenFoundById()
    {
        var patientId = await SeedPatientAsync("LookupById", "Test", "lookupbyid@example.com");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/test-data/patients/lookup?id={patientId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var patient = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal(patientId, patient!.Id);
        Assert.Equal("LookupById", patient.FirstName);
    }

    [Fact]
    public async Task LookupPatient_Returns200_WhenFoundByUid()
    {
        var uid = Guid.NewGuid();
        await SeedPatientWithUidAsync("LookupByUid", "Test", uid);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/test-data/patients/lookup?uid={uid}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var patient = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal("LookupByUid", patient!.FirstName);
    }

    [Fact]
    public async Task LookupPatient_Returns200_WhenFoundByEmailPrefix()
    {
        var uniquePrefix = $"lookupbyemail{Guid.NewGuid():N}"[..24];
        var email = $"{uniquePrefix}@example.com";
        await SeedPatientAsync("LookupByEmail", "Test", email);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/test-data/patients/lookup?email={uniquePrefix}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var patient = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal("LookupByEmail", patient!.FirstName);
    }

    // ---- GET /test-data/patients/random ----

    [Fact]
    public async Task GetRandomPatient_Returns404_WhenNoPatientsExist()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Patients.RemoveRange(db.Patients);
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/test-data/patients/random");

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRandomPatient_Returns200_WithPatientData_WhenPatientsExist()
    {
        await SeedPatientAsync("RandomFirst", "RandomLast", "randompatient@example.com");
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/patients/random");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var patient = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.True(patient!.Id > 0);
    }

    // ---- PUT /test-data/patients/{id} ----

    [Fact]
    public async Task UpdateTestPatient_Returns404_WhenPatientDoesNotExist()
    {
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync("/api/v1/test-data/patients/999999", new
        {
            firstName = "Ghost",
            lastName = "Patient",
            status = "Active"
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTestPatient_Returns400_WhenBodyIsNull()
    {
        var patientId = await SeedPatientAsync("NullBody", "Test", "nullbody@example.com");
        var client = _factory.CreateClient();

        var resp = await client.PutAsync(
            $"/api/v1/test-data/patients/{patientId}",
            new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTestPatient_Returns200_WithUpdatedFields()
    {
        var patientId = await SeedPatientAsync("BeforeFirst", "BeforeLast", "beforeupdate@example.com");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/test-data/patients/{patientId}", new
        {
            firstName = "AfterFirst",
            lastName = "AfterLast",
            status = "Inactive",
            city = "TestCity"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var updated = await resp.Content.ReadFromJsonAsync<PatientDto>();
        Assert.Equal("AfterFirst", updated!.FirstName);
        Assert.Equal("AfterLast", updated.LastName);
        Assert.Equal("Inactive", updated.Status);
        Assert.Equal("TestCity", updated.City);
    }

    // ---- GET /test-data/patients/stats ----

    [Fact]
    public async Task GetPatientStats_Returns200_WithValidStructure()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/test-data/patients/stats");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PatientStatsDto>(json, JsonOptions);
        Assert.NotNull(result);
        Assert.True(result!.PatientCount >= 0);
        Assert.True(result.DuplicatePatientCount >= 0);
        Assert.True(result.TotalStaffCount >= 0);
        Assert.NotNull(result.PatientsBySite);
    }

    [Fact]
    public async Task GetPatientStats_ReflectsSeededPatients()
    {
        await using var setupScope = _factory.Services.CreateAsyncScope();
        var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var countBefore = await setupDb.Patients.CountAsync();

        await SeedPatientAsync("StatsTest", "Patient", "statstest@example.com");

        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/test-data/patients/stats");

        resp.EnsureSuccessStatusCode();
        var result = JsonSerializer.Deserialize<PatientStatsDto>(
            await resp.Content.ReadAsStringAsync(), JsonOptions);
        Assert.Equal(countBefore + 1, result!.PatientCount);
    }

    // ---- POST /test-data/patients/generate ----

    [Fact]
    public async Task GeneratePatients_Returns400_WhenTotalCountIsZero()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/generate", new
        {
            totalCount = 0
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePatients_Returns400_WhenTotalCountIsNegative()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/generate", new
        {
            totalCount = -1
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePatients_Returns400_WhenDuplicatePercentageExceeds100()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/generate", new
        {
            totalCount = 5,
            duplicatePercentage = 101
        });

        await ApiErrorAssertions.AssertApiErrorAsync(resp, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePatients_Returns400_WhenDuplicatePercentageIsNegative()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/generate", new
        {
            totalCount = 5,
            duplicatePercentage = -1
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GeneratePatients_Returns200_WithCountSummary_WhenSmallCount()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/generate", new
        {
            totalCount = 3,
            duplicatePercentage = 0,
            seed = 42
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = JsonSerializer.Deserialize<GeneratePatientsResult>(
            await resp.Content.ReadAsStringAsync(), JsonOptions);
        Assert.Equal(3, result!.TotalRequested);
        Assert.Equal(3, result.TotalBaseInserted);
        Assert.True(result.TotalAfter >= 3);
    }

    [Fact]
    public async Task GeneratePatients_CreatesDuplicates_WhenDuplicatePercentageSet()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/patients/generate", new
        {
            totalCount = 10,
            duplicatePercentage = 10,
            seed = 99
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = JsonSerializer.Deserialize<GeneratePatientsResult>(
            await resp.Content.ReadAsStringAsync(), JsonOptions);
        Assert.True(result!.DuplicateInserted > 0);
    }

    // ---- Helpers ----

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

    private async Task SeedPatientWithUidAsync(string firstName, string lastName, Guid uid)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Patients.Add(new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            DisplayName = $"{lastName}, {firstName}",
            Status = "Active",
            Uid = uid
        });

        await db.SaveChangesAsync();
    }

    private sealed class AddPatientResult
    {
        public int Id { get; set; }
        public Guid Uid { get; set; }
    }

    private sealed class PatientDto
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Status { get; set; }
        public string? City { get; set; }
    }

    private sealed class PatientStatsDto
    {
        public int PatientCount { get; set; }
        public int DuplicatePatientCount { get; set; }
        public int RecentAuditEventCount { get; set; }
        public int TotalStaffCount { get; set; }
        public List<object>? PatientsBySite { get; set; }
    }

    private sealed class GeneratePatientsResult
    {
        public int TotalRequested { get; set; }
        public int TotalBaseInserted { get; set; }
        public int DuplicateRequested { get; set; }
        public int DuplicateInserted { get; set; }
        public int TotalAfter { get; set; }
    }
}
