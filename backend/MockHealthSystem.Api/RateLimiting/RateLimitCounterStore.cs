using System.Collections.Concurrent;

namespace MockHealthSystem.Api.RateLimiting;

public sealed class RateLimitCounterStore : IRateLimitCounterStore
{
    private static readonly long TicksPerSecond = TimeSpan.TicksPerSecond;
    private static readonly long TicksPerMinute = TimeSpan.TicksPerMinute;

    private readonly ConcurrentDictionary<string, PerIpCounters> _counters = new();

    public (bool Allowed, int RetryAfterSeconds) CheckAndIncrement(string ip, int perSecond, int perMinute)
    {
        var counter = _counters.GetOrAdd(ip, _ => new PerIpCounters());

        lock (counter)
        {
            var nowTick = DateTime.UtcNow.Ticks;
            // Roll second window if elapsed
            if (nowTick - counter.SecondWindowStartTick >= TicksPerSecond)
            {
                counter.SecondWindowStartTick = nowTick;
                counter.SecondCount = 0;
            }

            // Roll minute window if elapsed
            if (nowTick - counter.MinuteWindowStartTick >= TicksPerMinute)
            {
                counter.MinuteWindowStartTick = nowTick;
                counter.MinuteCount = 0;
            }

            bool secondExceeded = counter.SecondCount >= perSecond;
            bool minuteExceeded = counter.MinuteCount >= perMinute;

            if (secondExceeded || minuteExceeded)
            {
                int secondRetry = secondExceeded
                    ? Math.Max(1, (int)Math.Ceiling((double)(TicksPerSecond - (nowTick - counter.SecondWindowStartTick)) / TicksPerSecond))
                    : 0;
                int minuteRetry = minuteExceeded
                    ? Math.Max(1, (int)Math.Ceiling((double)(TicksPerMinute - (nowTick - counter.MinuteWindowStartTick)) / TicksPerSecond))
                    : 0;

                return (false, Math.Max(secondRetry, minuteRetry));
            }

            counter.SecondCount++;
            counter.MinuteCount++;
            return (true, 0);
        }
    }

    public void ResetAll() => _counters.Clear();
}
