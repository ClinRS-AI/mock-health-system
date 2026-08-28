using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MockHealthSystem.Tests.Integration;

/// <summary>
/// Simulates a direct external TCP peer (non-loopback) connecting to the app, the way a real
/// Kestrel deployment would present a caller. TestServer's in-memory transport otherwise reports
/// a null Connection.RemoteIpAddress, which trivially bypasses ForwardedHeadersMiddleware's
/// known-proxy check regardless of configuration, so it can't exercise this security path.
/// </summary>
public class ExternalPeerWebApplicationFactory : IsolatedWebApplicationFactory
{
    public const string ExternalPeerIp = "198.51.100.1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new FakeRemoteIpStartupFilter(ExternalPeerIp));
        });
    }

    private sealed class FakeRemoteIpStartupFilter(string remoteIp) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
                await nextMiddleware();
            });
            next(app);
        };
    }
}

/// <summary>
/// Same external-peer simulation, plus K_SERVICE set before the host builds so Program.cs treats
/// the deployment as Cloud Run and trusts X-Forwarded-For unconditionally.
/// </summary>
public sealed class ExternalPeerCloudRunSimulatedWebApplicationFactory : ExternalPeerWebApplicationFactory
{
    private readonly EnvironmentVariableScope _kServiceScope = new("K_SERVICE", "test-cloud-run-service");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _kServiceScope.Dispose();
        }
    }
}
