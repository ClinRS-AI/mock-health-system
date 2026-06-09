namespace MockHealthSystem.Api.RateLimiting;

public interface IRateLimitCounterStore
{
    /// <summary>
    /// Checks whether the given IP is within limits and, if so, increments its counters.
    /// </summary>
    /// <returns>
    /// Allowed = true when the request is within both limits.
    /// RetryAfterSeconds = seconds until the longest outstanding window resets (0 when Allowed).
    /// </returns>
    (bool Allowed, int RetryAfterSeconds) CheckAndIncrement(string ip, int perSecond, int perMinute);

    /// <summary>Clears all per-IP counters. Called when rate limit settings are saved.</summary>
    void ResetAll();
}
