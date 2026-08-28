using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Verifies Program.cs only trusts X-Forwarded-For unconditionally when K_SERVICE (the env var
/// Cloud Run sets on every revision) is present. Outside that topology, an arbitrary caller must
/// not be able to spoof the IP used for rate limiting and audit logging (RemoteIp) — the direct
/// TCP peer (simulated here since TestServer has no real socket) must win instead.
/// </summary>
public sealed class ForwardedHeadersTests
{
    private const string SpoofedIp = "203.0.113.9";

    [Fact]
    public async Task ForwardedFor_IsIgnored_OutsideCloudRun()
    {
        await using var factory = new ExternalPeerWebApplicationFactory();
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health");
        request.Headers.Add("X-Forwarded-For", SpoofedIp);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var remoteIp = await GetMostRecentRemoteIpAsync(factory);
        Assert.Equal(ExternalPeerWebApplicationFactory.ExternalPeerIp, remoteIp);
    }

    [Fact]
    public async Task ForwardedFor_IsTrusted_OnCloudRun()
    {
        await using var factory = new ExternalPeerCloudRunSimulatedWebApplicationFactory();
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health");
        request.Headers.Add("X-Forwarded-For", SpoofedIp);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var remoteIp = await GetMostRecentRemoteIpAsync(factory);
        Assert.Equal(SpoofedIp, remoteIp);
    }

    private static async Task<string?> GetMostRecentRemoteIpAsync(IsolatedWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await db.ApiRequestLogs
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstAsync();
        return log.RemoteIp;
    }
}
