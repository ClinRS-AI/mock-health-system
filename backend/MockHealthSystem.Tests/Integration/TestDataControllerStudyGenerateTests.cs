using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class TestDataControllerStudyGenerateTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerStudyGenerateTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateStudies_Returns200_AndInsertsRequestedCount()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/studies/generate", new { totalCount = 3, seed = 42 });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(3, doc.RootElement.GetProperty("totalInserted").GetInt32());
        Assert.True(doc.RootElement.GetProperty("armsInserted").GetInt32() > 0);
    }

    [Fact]
    public async Task GenerateStudies_AutoSeedsPrerequisiteLookups_WhenNoneExist()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(db.SponsorTeams);

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/test-data/studies/generate", new { totalCount = 1, seed = 7 });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.NotEmpty(db.SponsorTeams);
        Assert.NotEmpty(db.StudyCategories);
        Assert.NotEmpty(db.StudyStatusTypes);
    }

    [Fact]
    public async Task GenerateStudies_Returns400_WhenTotalCountExceedsMaximum()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/studies/generate", new { totalCount = 100000 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GenerateStudies_Returns400_WhenTotalCountIsZeroOrNegative()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/studies/generate", new { totalCount = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GenerateStudies_DefaultsTotalCountTo25()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/studies/generate", new { });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(25, doc.RootElement.GetProperty("totalRequested").GetInt32());
    }
}
