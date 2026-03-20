namespace MockHealthSystem.Infrastructure.Data.Entities;

/// <summary>
/// Authentication configuration for the mock health system.
/// Stored as a singleton row (Id = 1).
/// </summary>
public class AuthSettings
{
    public int Id { get; set; }

    /// <summary>
    /// Authentication mode: None, Bearer, or OAuth.
    /// </summary>
    public string Mode { get; set; } = "None";

    /// <summary>
    /// Shared secret for simple bearer token authentication.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Internal OAuth client identifier.
    /// </summary>
    public string? OAuthClientId { get; set; }

    /// <summary>
    /// Internal OAuth client secret (stored as plain text for this mock system).
    /// </summary>
    public string? OAuthClientSecret { get; set; }

    /// <summary>
    /// Access token lifetime in minutes for internal OAuth.
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token lifetime in days for internal OAuth.
    /// </summary>
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}

