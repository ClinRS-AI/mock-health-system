using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MockHealthSystem.Api.Services.AdminSession;

public sealed class AdminSessionJwtService : IAdminSessionJwtService
{
    public const string Issuer = "MockHealthSystem.Admin";
    public const string Audience = "MockHealthSystem.AdminUi";
    public const string AdminSessionClaimType = "mhs_admin_session";
    public const string AdminSessionClaimValue = "1";

    private readonly IConfiguration _configuration;
    private readonly IOptions<AdminSessionOptions> _options;
    private readonly TimeProvider _time;

    public AdminSessionJwtService(
        IConfiguration configuration,
        IOptions<AdminSessionOptions> options,
        TimeProvider time)
    {
        _configuration = configuration;
        _options = options;
        _time = time;
    }

    public AdminSessionMintResult? CreateSessionToken()
    {
        if (!TryGetSigningKeyBytes(out var keyBytes))
        {
            return null;
        }

        var ttl = TimeSpan.FromMinutes(Math.Clamp(_options.Value.TtlMinutes, 1, 1440));
        var now = _time.GetUtcNow().UtcDateTime;
        var expires = now.Add(ttl);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "admin-ui"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(AdminSessionClaimType, AdminSessionClaimValue),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var serialized = new JwtSecurityTokenHandler().WriteToken(token);
        return new AdminSessionMintResult(serialized, expires);
    }

    public bool TryValidateSessionToken(string token, out string? failureReason)
    {
        failureReason = null;
        if (string.IsNullOrWhiteSpace(token))
        {
            failureReason = "missing_token";
            return false;
        }

        if (!TryGetSigningKeyBytes(out var keyBytes))
        {
            failureReason = "signing_key_unavailable";
            return false;
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var validated);
            var jwt = (JwtSecurityToken)validated;
            var hasClaim = jwt.Claims.Any(c =>
                c.Type == AdminSessionClaimType && c.Value == AdminSessionClaimValue);
            if (!hasClaim)
            {
                failureReason = "invalid_claims";
                return false;
            }

            return true;
        }
        catch (SecurityTokenException ex)
        {
            failureReason = ex.Message;
            return false;
        }
    }

    private bool TryGetSigningKeyBytes(out byte[] keyBytes)
    {
        keyBytes = Array.Empty<byte>();

        var fromEnv = Environment.GetEnvironmentVariable("ADMIN_SESSION_SIGNING_KEY");
        var fromConfig = _configuration["AdminSession:SigningKey"];
        var explicitKey = !string.IsNullOrWhiteSpace(fromEnv) ? fromEnv.Trim() : fromConfig?.Trim();
        if (!string.IsNullOrEmpty(explicitKey))
        {
            keyBytes = Encoding.UTF8.GetBytes(explicitKey);
            return NormalizeKeyLength(ref keyBytes);
        }

        var adminKey = Environment.GetEnvironmentVariable("AUTH_SETTINGS_ADMIN_KEY");
        if (!string.IsNullOrWhiteSpace(adminKey))
        {
            keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(adminKey.Trim()));
            return true;
        }

        return false;
    }

    private static bool NormalizeKeyLength(ref byte[] keyBytes)
    {
        if (keyBytes.Length >= 32)
        {
            if (keyBytes.Length > 32)
            {
                keyBytes = SHA256.HashData(keyBytes);
            }

            return true;
        }

        if (keyBytes.Length == 0)
        {
            return false;
        }

        keyBytes = SHA256.HashData(keyBytes);
        return true;
    }
}
