namespace MockHealthSystem.Api.Services.AdminSession;

/// <summary>
/// Options for short-lived admin UI JWTs, bound from configuration section <see cref="SectionName"/> (e.g. appsettings
/// <c>AdminSession:TtlMinutes</c> / <c>SigningKey</c>). Environment uses the double-underscore convention:
/// <c>AdminSession__TtlMinutes</c>, <c>AdminSession__SigningKey</c>. For signing only, <see cref="AdminSessionJwtService"/>
/// also accepts <c>ADMIN_SESSION_SIGNING_KEY</c> as an alternate env source (TTL always comes from these options).
/// </summary>
public sealed class AdminSessionOptions
{
    public const string SectionName = "AdminSession";

    /// <summary>
    /// Symmetric signing key for HS256 (binds from appsettings or <c>AdminSession__SigningKey</c>).
    /// <see cref="AdminSessionJwtService"/> also accepts env <c>ADMIN_SESSION_SIGNING_KEY</c> as an alternate source.
    /// </summary>
    public string SigningKey { get; set; } = "";

    /// <summary>Access token lifetime in minutes (default 30).</summary>
    public int TtlMinutes { get; set; } = 30;
}
