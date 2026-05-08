using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class SystemControllerTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SystemControllerTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- GET /system/conditions/odata ----

    [Fact]
    public async Task GetConditionsOdata_Returns200_WithValidResponseShape()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/system/conditions/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await DeserializeOdataResult<ConditionItem>(resp);
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        Assert.True(result.Count >= 0);
    }

    [Fact]
    public async Task GetConditionsOdata_Returns200_WithSeededConditions()
    {
        await SeedConditionsAsync(3);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/system/conditions/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await DeserializeOdataResult<ConditionItem>(resp);
        Assert.NotNull(result?.Items);
        Assert.True(result!.Items.Count >= 3);
        Assert.True(result.Count >= 3);
    }

    [Fact]
    public async Task GetConditionsOdata_RespectsTopPaging()
    {
        await SeedConditionsAsync(5);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/system/conditions/odata?top=2");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await DeserializeOdataResult<ConditionItem>(resp);
        Assert.Equal(2, result!.Items.Count);
        Assert.True(result.Count >= 5); // total count is unaffected by paging
    }

    [Fact]
    public async Task GetConditionsOdata_RespectsSkipPaging()
    {
        await SeedConditionsAsync(4);
        var client = _factory.CreateClient();

        var allResp = await client.GetAsync("/api/v1/system/conditions/odata?top=100");
        var allResult = await DeserializeOdataResult<ConditionItem>(allResp);

        var skipResp = await client.GetAsync("/api/v1/system/conditions/odata?skip=1&top=100");
        var skipResult = await DeserializeOdataResult<ConditionItem>(skipResp);

        Assert.Equal(allResult!.Items.Count - 1, skipResult!.Items.Count);
        Assert.Equal(allResult.Items[1].Id, skipResult.Items[0].Id);
    }

    [Fact]
    public async Task GetConditionsOdata_ClipsNegativeSkipToZero()
    {
        await SeedConditionsAsync(2);
        var client = _factory.CreateClient();

        var withNegativeSkip = await client.GetAsync("/api/v1/system/conditions/odata?skip=-5&top=100");
        var withZeroSkip = await client.GetAsync("/api/v1/system/conditions/odata?skip=0&top=100");

        Assert.Equal(HttpStatusCode.OK, withNegativeSkip.StatusCode);
        var negResult = await DeserializeOdataResult<ConditionItem>(withNegativeSkip);
        var zeroResult = await DeserializeOdataResult<ConditionItem>(withZeroSkip);
        Assert.Equal(zeroResult!.Items.Count, negResult!.Items.Count);
    }

    [Fact]
    public async Task GetConditionsOdata_DefaultsTopTo100_WhenZeroOrNegative()
    {
        var client = _factory.CreateClient();

        // top=0 should be treated as the default (100)
        var resp = await client.GetAsync("/api/v1/system/conditions/odata?top=0");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await DeserializeOdataResult<ConditionItem>(resp);
        Assert.NotNull(result);
    }

    // ---- GET /system/medications/odata ----

    [Fact]
    public async Task GetMedicationsOdata_Returns200_WithValidResponseShape()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/system/medications/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await DeserializeOdataResult<MedicationItem>(resp);
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
    }

    [Fact]
    public async Task GetMedicationsOdata_Returns200_WithSeededMedications()
    {
        await SeedMedicationsAsync(2);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/system/medications/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await DeserializeOdataResult<MedicationItem>(resp);
        Assert.True(result!.Items.Count >= 2);
    }

    [Fact]
    public async Task GetMedicationsOdata_RespectsTopPaging()
    {
        await SeedMedicationsAsync(3);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/system/medications/odata?top=1");

        var result = await DeserializeOdataResult<MedicationItem>(resp);
        Assert.Single(result!.Items);
    }

    // ---- GET /system/allergies/odata ----

    [Fact]
    public async Task GetAllergiesOdata_Returns200_WithValidResponseShape()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/system/allergies/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await DeserializeOdataResult<AllergyItem>(resp);
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
    }

    [Fact]
    public async Task GetAllergiesOdata_Returns200_WithSeededAllergies()
    {
        await SeedAllergiesAsync(2);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/system/allergies/odata");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await DeserializeOdataResult<AllergyItem>(resp);
        Assert.True(result!.Items.Count >= 2);
    }

    [Fact]
    public async Task GetAllergiesOdata_RespectsTopPaging()
    {
        await SeedAllergiesAsync(3);
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/system/allergies/odata?top=1");

        var result = await DeserializeOdataResult<AllergyItem>(resp);
        Assert.Single(result!.Items);
    }

    // ---- Helpers ----

    private async Task SeedConditionsAsync(int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = db.Conditions.Count();
        for (var i = existing; i < count; i++)
        {
            db.Conditions.Add(new Condition { Name = $"Condition-{i}" });
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedMedicationsAsync(int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = db.Medications.Count();
        for (var i = existing; i < count; i++)
        {
            db.Medications.Add(new Medication { Name = $"Medication-{i}" });
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedAllergiesAsync(int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = db.Allergies.Count();
        for (var i = existing; i < count; i++)
        {
            db.Allergies.Add(new Allergy { Name = $"Allergy-{i}" });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<ODataResult<T>?> DeserializeOdataResult<T>(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ODataResult<T>>(json, JsonOptions);
    }

    private sealed class ODataResult<T>
    {
        public List<T> Items { get; set; } = [];
        public long Count { get; set; }
        public string? NextPageLink { get; set; }
    }

    private sealed class ConditionItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class MedicationItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class AllergyItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
