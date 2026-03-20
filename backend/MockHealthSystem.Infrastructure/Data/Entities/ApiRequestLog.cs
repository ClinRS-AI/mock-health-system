namespace MockHealthSystem.Infrastructure.Data.Entities;

public class ApiRequestLog
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

