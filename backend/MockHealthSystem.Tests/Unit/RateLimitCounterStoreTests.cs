using MockHealthSystem.Api.RateLimiting;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public class RateLimitCounterStoreTests
{
    private static RateLimitCounterStore CreateStore() => new();

    [Fact]
    public void NewIp_FirstRequest_IsAllowed()
    {
        var store = CreateStore();
        var (allowed, retry) = store.CheckAndIncrement("1.2.3.4", 10, 300);
        Assert.True(allowed);
        Assert.Equal(0, retry);
    }

    [Fact]
    public void WithinPerSecondLimit_IsAllowed()
    {
        var store = CreateStore();
        for (var i = 0; i < 5; i++)
        {
            var (allowed, _) = store.CheckAndIncrement("1.2.3.4", 10, 300);
            Assert.True(allowed);
        }
    }

    [Fact]
    public void AtPerSecondLimit_NextRequestIsRejected()
    {
        var store = CreateStore();
        for (var i = 0; i < 3; i++)
            store.CheckAndIncrement("1.2.3.4", 3, 300);

        var (allowed, retry) = store.CheckAndIncrement("1.2.3.4", 3, 300);
        Assert.False(allowed);
        Assert.True(retry >= 1);
    }

    [Fact]
    public void AtPerMinuteLimit_NextRequestIsRejected()
    {
        var store = CreateStore();
        for (var i = 0; i < 5; i++)
            store.CheckAndIncrement("1.2.3.4", 100, 5);

        var (allowed, retry) = store.CheckAndIncrement("1.2.3.4", 100, 5);
        Assert.False(allowed);
        Assert.True(retry >= 1);
    }

    [Fact]
    public void DifferentIps_HaveIndependentCounters()
    {
        var store = CreateStore();
        // Exhaust IP A
        for (var i = 0; i < 2; i++)
            store.CheckAndIncrement("10.0.0.1", 2, 300);

        var (allowedA, _) = store.CheckAndIncrement("10.0.0.1", 2, 300);
        var (allowedB, _) = store.CheckAndIncrement("10.0.0.2", 2, 300);

        Assert.False(allowedA);
        Assert.True(allowedB);
    }

    [Fact]
    public void ResetAll_ClearsCounters()
    {
        var store = CreateStore();
        // Fill up the limit
        for (var i = 0; i < 2; i++)
            store.CheckAndIncrement("1.2.3.4", 2, 300);

        var (blockedBefore, _) = store.CheckAndIncrement("1.2.3.4", 2, 300);
        Assert.False(blockedBefore);

        store.ResetAll();

        var (allowedAfter, _) = store.CheckAndIncrement("1.2.3.4", 2, 300);
        Assert.True(allowedAfter);
    }

    [Fact]
    public void BothLimitsExceeded_RetryAfterIsLongerWindow()
    {
        // Set per-second = 1, per-minute = 1 → both exhausted immediately
        var store = CreateStore();
        store.CheckAndIncrement("1.2.3.4", 1, 1); // uses both limits

        var (allowed, retry) = store.CheckAndIncrement("1.2.3.4", 1, 1);
        Assert.False(allowed);
        // Minute window resets in up to 60s; second window in up to 1s.
        // RetryAfterSeconds must reflect the minute window (the longer one).
        Assert.True(retry > 1, $"Expected retry > 1 (minute window), got {retry}");
    }

    [Fact]
    public void OnlyPerSecondExceeded_RetryAfterIsAtMostOneSecond()
    {
        // per-minute is large so only per-second fires
        var store = CreateStore();
        store.CheckAndIncrement("1.2.3.4", 1, 1000);

        var (allowed, retry) = store.CheckAndIncrement("1.2.3.4", 1, 1000);
        Assert.False(allowed);
        Assert.Equal(1, retry);
    }

    [Fact]
    public void OnlyPerMinuteExceeded_RetryAfterReflectsMinuteWindow()
    {
        // per-second is large so only per-minute fires
        var store = CreateStore();
        store.CheckAndIncrement("1.2.3.4", 1000, 1);

        var (allowed, retry) = store.CheckAndIncrement("1.2.3.4", 1000, 1);
        Assert.False(allowed);
        Assert.True(retry > 1, $"Expected retry > 1 (minute window), got {retry}");
    }

    [Fact]
    public void RetryAfterSeconds_IsNeverZero_WhenRejected()
    {
        var store = CreateStore();
        store.CheckAndIncrement("1.2.3.4", 1, 1000);

        var (allowed, retry) = store.CheckAndIncrement("1.2.3.4", 1, 1000);
        Assert.False(allowed);
        Assert.True(retry >= 1);
    }
}
