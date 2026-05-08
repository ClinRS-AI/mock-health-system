namespace MockHealthSystem.Api.Monitoring;

/// <summary>Centralizes monitoring list sizing (see monitoring requests endpoint) so limits are testable and consistent.</summary>
public static class MonitoringRequestListLimits
{
    public const int DefaultTake = 100;
    public const int MaxTake = 500;

    public static int ClampTake(int? take) =>
        take is null or <= 0 ? DefaultTake : Math.Min(take.Value, MaxTake);
}
