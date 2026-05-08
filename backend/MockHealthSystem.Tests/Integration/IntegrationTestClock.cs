namespace MockHealthSystem.Tests.Integration;

/// <summary>Fixed UTC timestamps for integration tests that seed rows ordered by time (avoids flaky ordering and midnight boundaries).</summary>
internal static class IntegrationTestClock
{
    /// <summary>Stable epoch; increment with <see cref="Step"/> for monotonic ordering.</summary>
    public static readonly DateTime UtcEpoch = new(2026, 5, 7, 14, 0, 0, DateTimeKind.Utc);

    public static DateTime Step(int offsetSeconds) => UtcEpoch.AddSeconds(offsetSeconds);
}
