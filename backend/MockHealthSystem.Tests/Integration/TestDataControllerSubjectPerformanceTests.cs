using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>Verifies spec SC-004: a representative batch generates in under 30 seconds.</summary>
public sealed class TestDataControllerSubjectPerformanceTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectPerformanceTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateSubjects_DefaultBatch_CompletesInUnderThirtySeconds()
    {
        // Seed enough patients/studies that a batch of 25 is actually achievable.
        for (var i = 0; i < 10; i++)
        {
            await SubjectSeedHelpers.SeedPrerequisitesAsync(_factory);
        }

        var client = _factory.CreateClient();
        var stopwatch = Stopwatch.StartNew();

        var resp = await client.PostAsJsonAsync("/api/v1/test-data/subjects/generate", new { totalCount = 25, seed = 99 });

        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"Generation took {stopwatch.Elapsed.TotalSeconds:F1}s, expected < 30s (spec SC-004).");
    }
}
