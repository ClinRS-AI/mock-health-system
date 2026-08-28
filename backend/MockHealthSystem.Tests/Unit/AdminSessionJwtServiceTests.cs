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

    private static AdminSessionJwtService CreateService(IConfiguration? config = null) =>
        new(
            config ?? new ConfigurationBuilder().AddInMemoryCollection().Build(),
            Options.Create(new AdminSessionOptions { TtlMinutes = 30 }),
            new FakeTimeProvider(DateTimeOffset.UtcNow));

    [Fact]
    public void CreateSessionToken_ReturnsNull_WhenAdminKeyShorterThanMinimumAndNoExplicitSigningKey()
    {
        using var adminKey = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "short-key");
        using var signingKey = new EnvironmentVariableScope("ADMIN_SESSION_SIGNING_KEY", null);
        Assert.True("short-key".Length < AdminSessionJwtService.MinimumAdminKeyLengthForDerivedSigningKey);

        var sut = CreateService();

        Assert.Null(sut.CreateSessionToken());
    }

    [Fact]
    public void TryValidateSessionToken_Fails_WhenAdminKeyShorterThanMinimumAndNoExplicitSigningKey()
    {
        using var adminKey = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "short-key");
        using var signingKey = new EnvironmentVariableScope("ADMIN_SESSION_SIGNING_KEY", null);
        var sut = CreateService();

        var ok = sut.TryValidateSessionToken("irrelevant-token", out var failureReason);

        Assert.False(ok);
        Assert.Equal("signing_key_unavailable", failureReason);
    }

    [Fact]
    public void CreateSessionToken_Succeeds_WhenAdminKeyMeetsMinimumLength()
    {
        var adminKeyValue = new string('a', AdminSessionJwtService.MinimumAdminKeyLengthForDerivedSigningKey);
        using var adminKey = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", adminKeyValue);
        using var signingKey = new EnvironmentVariableScope("ADMIN_SESSION_SIGNING_KEY", null);
        var sut = CreateService();

        Assert.NotNull(sut.CreateSessionToken());
    }

    [Fact]
    public void CreateSessionToken_Succeeds_WhenExplicitSigningKeySet_EvenIfAdminKeyTooShort()
    {
        using var adminKey = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "sk");
        using var signingKey = new EnvironmentVariableScope(
            "ADMIN_SESSION_SIGNING_KEY", "a-sufficiently-long-explicit-signing-key");
        var sut = CreateService();

        Assert.NotNull(sut.CreateSessionToken());
    }
}
