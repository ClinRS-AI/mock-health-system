using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// Internal OAuth-style token endpoints for the mock authentication system.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthSettingsService _authSettingsService;

    public AuthController(AppDbContext db, IAuthSettingsService authSettingsService)
    {
        _db = db;
        _authSettingsService = authSettingsService;
    }

    /// <summary>
    /// Verifies that the request is authenticated. Returns 200 if the configured authentication method succeeds; otherwise 401.
    /// Use this to confirm credentials (e.g. Bearer token or OAuth access token) are valid.
    /// </summary>
    [HttpGet("verify")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Verify()
    {
        return Ok();
    }

    /// <summary>
    /// Issues an access token and refresh token using simple client credentials when OAuth mode is enabled.
    /// </summary>
    /// <param name="request">Client credentials and optional subject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access and refresh tokens.</returns>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateToken([FromBody] TokenRequest request, CancellationToken cancellationToken)
    {
        var settings = await _authSettingsService.GetSettingsAsync(cancellationToken);
        if (!string.Equals(settings.Mode, "OAuth", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("OAuth mode is not enabled.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return BadRequest("clientId and clientSecret are required.");
        }

        if (!string.Equals(request.ClientId, settings.OAuthClientId, StringComparison.Ordinal) ||
            !string.Equals(request.ClientSecret, settings.OAuthClientSecret, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var accessTokenLifetime = TimeSpan.FromMinutes(settings.AccessTokenLifetimeMinutes);
        var refreshTokenLifetime = TimeSpan.FromDays(settings.RefreshTokenLifetimeDays);

        var accessTokenValue = Guid.NewGuid().ToString("N");
        var refreshTokenValue = Guid.NewGuid().ToString("N");

        var accessToken = new AuthToken
        {
            Token = accessTokenValue,
            TokenType = "access",
            ClientId = settings.OAuthClientId ?? request.ClientId,
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject,
            CreatedAt = now,
            ExpiresAt = now.Add(accessTokenLifetime)
        };

        var refreshToken = new AuthToken
        {
            Token = refreshTokenValue,
            TokenType = "refresh",
            ClientId = settings.OAuthClientId ?? request.ClientId,
            Subject = accessToken.Subject,
            CreatedAt = now,
            ExpiresAt = now.Add(refreshTokenLifetime)
        };

        _db.AuthTokens.Add(accessToken);
        _db.AuthTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new TokenResponse(
            AccessToken: accessTokenValue,
            TokenType: "Bearer",
            ExpiresIn: (int)accessTokenLifetime.TotalSeconds,
            RefreshToken: refreshTokenValue);

        return Ok(response);
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access token when OAuth mode is enabled.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New access token and the same refresh token.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("refreshToken is required.");
        }

        var settings = await _authSettingsService.GetSettingsAsync(cancellationToken);
        if (!string.Equals(settings.Mode, "OAuth", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("OAuth mode is not enabled.");
        }

        var now = DateTime.UtcNow;
        var existingRefresh = await _db.AuthTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Token == request.RefreshToken &&
                     t.TokenType == "refresh" &&
                     t.RevokedAt == null &&
                     t.ExpiresAt > now,
                cancellationToken);

        if (existingRefresh is null)
        {
            return Unauthorized();
        }

        var accessTokenLifetime = TimeSpan.FromMinutes(settings.AccessTokenLifetimeMinutes);
        var newAccessTokenValue = Guid.NewGuid().ToString("N");

        var newAccessToken = new AuthToken
        {
            Token = newAccessTokenValue,
            TokenType = "access",
            ClientId = existingRefresh.ClientId,
            Subject = existingRefresh.Subject,
            CreatedAt = now,
            ExpiresAt = now.Add(accessTokenLifetime)
        };

        _db.AuthTokens.Add(newAccessToken);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new TokenResponse(
            AccessToken: newAccessTokenValue,
            TokenType: "Bearer",
            ExpiresIn: (int)accessTokenLifetime.TotalSeconds,
            RefreshToken: request.RefreshToken);

        return Ok(response);
    }

    /// <summary>
    /// Token request for client credentials flow.
    /// </summary>
    public sealed class TokenRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string? Subject { get; set; }
    }

    /// <summary>
    /// Refresh token request body.
    /// </summary>
    public sealed class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Standard token response payload.
    /// </summary>
    public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, string RefreshToken);
}

