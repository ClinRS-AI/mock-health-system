using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class TestDataControllerSubjectGenerateTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectGenerateTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateSubjects_Returns200_AndInsertsRequestedCount()
    {
        await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/subjects/generate", new { totalCount = 1, seed = 42 });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("totalInserted").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("statusHistoryInserted").GetInt32());
    }

    [Fact]
    public async Task GenerateSubjects_Returns400_WhenTotalCountExceedsMaximum()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/subjects/generate", new { totalCount = 501 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GenerateSubjects_Returns400_WhenTotalCountIsZeroOrNegative()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/subjects/generate", new { totalCount = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GenerateSubjects_DefaultsTotalCountTo25()
    {
        await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/subjects/generate", new { });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(25, doc.RootElement.GetProperty("totalRequested").GetInt32());
    }
}

/// <summary>Isolated in its own class (own factory/DB instance — see IsolatedWebApplicationFactory's
/// "one DB per test class" doc comment) because this assertion depends on an exact, small
/// candidate pool that other tests sharing a class-level fixture would otherwise pollute.</summary>
public sealed class TestDataControllerSubjectGenerateLimitedCandidatesTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectGenerateLimitedCandidatesTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateSubjects_ReportsActualInsertedCount_WhenFewerValidCombinationsThanRequested()
    {
        await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Patients.Add(new Patient { Uid = Guid.NewGuid(), FirstName = "Second", LastName = "Patient", Status = "Active" });
            await db.SaveChangesAsync();
        }
        var client = _factory.CreateClient();

        // 2 patients x 1 study = 2 valid pairs max, request far more.
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/subjects/generate", new { totalCount = 20, seed = 42 });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(20, doc.RootElement.GetProperty("totalRequested").GetInt32());
        Assert.True(doc.RootElement.GetProperty("totalInserted").GetInt32() <= 2);
    }
}

/// <summary>Isolated in its own class for a genuinely empty (no patients) starting DB.</summary>
public sealed class TestDataControllerSubjectGenerateNoPatientsTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectGenerateNoPatientsTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateSubjects_Returns400_WhenNoPatientsExist()
    {
        await StudySeedHelpers.SeedStudyAsync(_factory, "Study Without Patients");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/subjects/generate", new { totalCount = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}

/// <summary>Isolated in its own class for a genuinely empty (no studies) starting DB.</summary>
public sealed class TestDataControllerSubjectGenerateNoStudiesTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectGenerateNoStudiesTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateSubjects_Returns400_WhenNoStudiesExist()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Patients.Add(new Patient { Uid = Guid.NewGuid(), FirstName = "Lonely", LastName = "Patient", Status = "Active" });
            await db.SaveChangesAsync();
        }
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/subjects/generate", new { totalCount = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
