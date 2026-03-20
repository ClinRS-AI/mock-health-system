namespace MockHealthSystem.Api.Models.Monitoring;

public sealed class ApiRequestLogSummaryModel
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int DurationMs { get; set; }
    public string? Origin { get; set; }
}

/// <summary>
/// Status code count for monitoring stats (pie chart).
/// </summary>
public sealed class StatusBreakdownItem
{
    public int StatusCode { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Aggregated stats for the last N requests (e.g. 200) for dashboard visualizations.
/// </summary>
public sealed class MonitoringStatsModel
{
    public IReadOnlyList<StatusBreakdownItem> StatusBreakdown { get; set; } = Array.Empty<StatusBreakdownItem>();
    public int RequestCount { get; set; }
    public double? AverageDurationMs { get; set; }
    public double? Percentile95DurationMs { get; set; }
    public int? MaxDurationMs { get; set; }
}

public sealed class ApiRequestLogDetailModel
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public int StatusCode { get; set; }
    public int DurationMs { get; set; }
    public string? Origin { get; set; }
    public string? Referer { get; set; }
    public string? UserAgent { get; set; }
    public string? RemoteIp { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? CorrelationId { get; set; }
}

