namespace MockHealthSystem.Infrastructure.Data.Entities;

/// <summary>
/// Opaque access or refresh token for the internal OAuth mode.
/// </summary>
public class AuthToken
{
    public int Id { get; set; }

    /// <summary>
    /// Token value returned to clients (e.g. a GUID string).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token type: \"Access\" or \"Refresh\".
    /// </summary>
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// OAuth client identifier this token was issued for.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Subject this token represents (e.g. external system name); optional.
    /// </summary>
    public string? Subject { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}

