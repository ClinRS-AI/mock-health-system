using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MockHealthSystem.Api.Services.AdminSession;
using MockHealthSystem.Tests.Integration;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class AdminSessionJwtServiceTests
{
    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    [Fact]
    public void TryValidateSessionToken_Fails_WhenTokenExpired()
    {
        using var env = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "expiry-signing-key");
        var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(anchor);
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var options = Options.Create(new AdminSessionOptions { TtlMinutes = 1 });
        var sut = new AdminSessionJwtService(config, options, clock);

        var minted = sut.CreateSessionToken();
        Assert.NotNull(minted);

        clock.SetUtcNow(anchor.AddHours(2));

        Assert.False(sut.TryValidateSessionToken(minted.AccessToken, out _));
    }
}
