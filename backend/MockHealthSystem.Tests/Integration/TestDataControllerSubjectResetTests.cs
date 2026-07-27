using System.Net;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// ResetSubjectsAsync uses raw SQL TRUNCATE (matching ResetStudiesAsync/ResetPatientsAsync's
/// existing pattern), which the in-memory test provider does not support — it surfaces as a 500
/// via ExceptionHandlingMiddleware. This mirrors TestDataControllerStudyResetTests's accepted
/// precedent — the happy path (200 OK, tables actually cleared) is only exercised against real
/// PostgreSQL (see quickstart.md / T049's live-Postgres verification), not this in-memory suite.
/// </summary>
public sealed class TestDataControllerSubjectResetTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public TestDataControllerSubjectResetTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResetSubjects_ReturnsInternalServerError_AgainstInMemoryProvider()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsync("/api/v1/test-data/subjects/reset", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
    }
}
