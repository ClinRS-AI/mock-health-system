namespace MockHealthSystem.Api.Services.AdminSession;

/// <summary>
/// Options for short-lived admin UI JWTs. Environment overrides: AdminSession__TtlMinutes, AdminSession__SigningKey,
/// or ADMIN_SESSION_SIGNING_KEY / ADMIN_SESSION_TTL_MINUTES (read in <see cref="AdminSessionJwtService"/>).
/// </summary>
public sealed class AdminSessionOptions
{
    public const string SectionName = "AdminSession";

    /// <summary>Symmetric signing key for HS256. Prefer ADMIN_SESSION_SIGNING_KEY in production.</summary>
    public string SigningKey { get; set; } = "";

    /// <summary>Access token lifetime in minutes (default 30).</summary>
    public int TtlMinutes { get; set; } = 30;
}
