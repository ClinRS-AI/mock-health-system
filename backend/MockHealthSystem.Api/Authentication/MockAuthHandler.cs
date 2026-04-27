using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;

namespace MockHealthSystem.Api.Authentication;

public sealed class MockAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IAuthSettingsService _authSettingsService;
    private readonly AppDbContext _dbContext;

    public MockAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAuthSettingsService authSettingsService,
        AppDbContext dbContext)
        : base(options, logger, encoder)
    {
        _authSettingsService = authSettingsService;
        _dbContext = dbContext;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var settings = await _authSettingsService.GetSettingsAsync(Context.RequestAborted);
        var mode = settings.Mode?.Trim();

        if (string.Equals(mode, "None", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(mode))
        {
            var identity = new ClaimsIdentity(Scheme.Name);
            identity.AddClaim(new Claim(ClaimTypes.Name, "mock-anonymous"));
            var principal = new ClaimsPrincipal(identity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        if (string.Equals(mode, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.Fail("Missing or invalid Authorization header.");
            }

            var token = authHeader["Bearer ".Length..].Trim();
            if (string.IsNullOrEmpty(settings.BearerToken))
            {
                return AuthenticateResult.Fail("Bearer token is not configured.");
            }

            if (!string.Equals(token, settings.BearerToken, StringComparison.Ordinal))
            {
                return AuthenticateResult.Fail("Invalid bearer token.");
            }

            var identity = new ClaimsIdentity(Scheme.Name);
            identity.AddClaim(new Claim(ClaimTypes.Name, "mock-bearer-client"));
            var principal = new ClaimsPrincipal(identity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        if (string.Equals(mode, "CCAPIKey", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(settings.BearerToken))
            {
                return AuthenticateResult.Fail("CCAPIKey secret is not configured.");
            }

            if (!Request.Headers.TryGetValue("CCAPIKey", out var headerValues))
            {
                return AuthenticateResult.Fail("Missing CCAPIKey header.");
            }

            var apiKey = headerValues.ToString().Trim();
            if (!string.Equals(apiKey, settings.BearerToken, StringComparison.Ordinal))
            {
                return AuthenticateResult.Fail("Invalid CCAPIKey value.");
            }

            var identity = new ClaimsIdentity(Scheme.Name);
            identity.AddClaim(new Claim(ClaimTypes.Name, "mock-ccapi-client"));
            var principal = new ClaimsPrincipal(identity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        if (string.Equals(mode, "OAuth", StringComparison.OrdinalIgnoreCase))
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.Fail("Missing or invalid Authorization header.");
            }

            var token = authHeader["Bearer ".Length..].Trim();
            var now = DateTime.UtcNow;

            var tokenEntity = await _dbContext.AuthTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Token == token &&
                         t.TokenType == "access" &&
                         t.RevokedAt == null &&
                         t.ExpiresAt > now,
                    Context.RequestAborted);

            if (tokenEntity is null)
            {
                return AuthenticateResult.Fail("Invalid or expired access token.");
            }

            var identity = new ClaimsIdentity(Scheme.Name);
            var subject = string.IsNullOrWhiteSpace(tokenEntity.Subject) ? tokenEntity.ClientId : tokenEntity.Subject;
            identity.AddClaim(new Claim(ClaimTypes.Name, subject));
            identity.AddClaim(new Claim("client_id", tokenEntity.ClientId));
            var principal = new ClaimsPrincipal(identity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        return AuthenticateResult.Fail("Unsupported authentication mode.");
    }
}

