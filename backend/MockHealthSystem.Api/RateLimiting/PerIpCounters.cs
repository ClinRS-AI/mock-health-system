namespace MockHealthSystem.Api.RateLimiting;

/// <summary>
/// Fixed-window request counters for a single IP address. Lock on this instance before reading or writing.
/// </summary>
public sealed class PerIpCounters
{
    public long SecondWindowStartTick;
    public int SecondCount;
    public long MinuteWindowStartTick;
    public int MinuteCount;
}
