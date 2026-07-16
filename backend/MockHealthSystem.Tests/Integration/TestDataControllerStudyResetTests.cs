using System.Net;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// ResetStudiesAsync uses raw SQL TRUNCATE (matching ResetPatientsAsync's existing pattern),
/// which the in-memory test provider does not support — it surfaces as a 500 via
/// ExceptionHandlingMiddleware rather than the InMemory-unsupported-API exception crashing the
/// request. This mirrors the existing, accepted precedent for patients/reset
/// (TestDataAdminAndAuditEdgeTests) — the happy path (200 OK, tables actually cleared) is only
/// exercised against real PostgreSQL, not covered by this in-memory suite.
/// </summary>
public sealed class TestDataControllerStudyResetTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerStudyResetTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResetStudies_ReturnsInternalServerError_AgainstInMemoryProvider()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsync("/api/v1/test-data/studies/reset", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    [Fact]
    public async Task ResetStudies_WithIncludeLookups_ReturnsInternalServerError_AgainstInMemoryProvider()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsync("/api/v1/test-data/studies/reset?includeLookups=true", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
    }
}
