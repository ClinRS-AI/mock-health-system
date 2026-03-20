namespace MockHealthSystem.Api.Models;

/// <summary>
/// Consistent error response body for API failures.
/// </summary>
public sealed class ApiErrorResponse
{
    /// <summary>HTTP status code.</summary>
    public int Status { get; init; }

    /// <summary>Short error title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Optional detail message.</summary>
    public string? Detail { get; init; }

    /// <summary>Request trace id for correlation.</summary>
    public string? TraceId { get; init; }
}
