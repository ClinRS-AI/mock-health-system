namespace MockHealthSystem.Api.Services.AdminSession;

public sealed record AdminSessionMintResult(string AccessToken, DateTime ExpiresAtUtc);

public interface IAdminSessionJwtService
{
    /// <summary>Returns null if signing material cannot be resolved.</summary>
    AdminSessionMintResult? CreateSessionToken();

    bool TryValidateSessionToken(string token, out string? failureReason);
}
