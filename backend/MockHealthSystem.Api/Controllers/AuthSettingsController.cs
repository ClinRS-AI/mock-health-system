using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Auth;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// Administration endpoints for managing authentication settings.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth-settings")]
[AllowAnonymous]
public sealed class AuthSettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthSettingsService _authSettingsService;

    public AuthSettingsController(AppDbContext db, IAuthSettingsService authSettingsService)
    {
        _db = db;
        _authSettingsService = authSettingsService;
    }

    /// <summary>
    /// Returns the current authentication settings.
    /// </summary>
    /// <returns>Authentication settings and summary info.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AuthSettingsViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        var settings = await _authSettingsService.GetSettingsAsync(cancellationToken);

        var hasTokens = await _db.AuthTokens
            .AsNoTracking()
            .AnyAsync(cancellationToken);

        var viewModel = new AuthSettingsViewModel
        {
            Mode = settings.Mode,
            BearerToken = settings.BearerToken,
            OAuthClientId = settings.OAuthClientId,
            OAuthClientSecret = settings.OAuthClientSecret,
            AccessTokenLifetimeMinutes = settings.AccessTokenLifetimeMinutes,
            RefreshTokenLifetimeDays = settings.RefreshTokenLifetimeDays,
            HasAnyTokens = hasTokens
        };

        return Ok(viewModel);
    }

    /// <summary>
    /// Updates the global authentication settings.
    /// </summary>
    /// <param name="model">Updated settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated settings.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(AuthSettingsViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsync([FromBody] AuthSettingsUpdateModel model, CancellationToken cancellationToken)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(model.Mode))
        {
            return BadRequest("Mode is required.");
        }

        var normalizedMode = model.Mode.Trim();
        if (!string.Equals(normalizedMode, "None", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedMode, "Bearer", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedMode, "CCAPIKey", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedMode, "OAuth", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Mode must be one of: None, Bearer, CCAPIKey, OAuth.");
        }

        var existing = await _db.AuthSettings.FirstOrDefaultAsync(cancellationToken);
        var created = false;
        if (existing is null)
        {
            existing = new Infrastructure.Data.Entities.AuthSettings
            {
                Id = 1,
                Mode = "None",
                AccessTokenLifetimeMinutes = 60,
                RefreshTokenLifetimeDays = 30
            };
            _db.AuthSettings.Add(existing);
            created = true;
        }

        var previousMode = existing.Mode;

        existing.Mode = normalizedMode;

        if (model.BearerToken is not null)
        {
            existing.BearerToken = model.BearerToken;
        }

        if (model.OAuthClientId is not null)
        {
            existing.OAuthClientId = model.OAuthClientId;
        }

        if (model.OAuthClientSecret is not null)
        {
            existing.OAuthClientSecret = model.OAuthClientSecret;
        }

        if (model.AccessTokenLifetimeMinutes.HasValue)
        {
            existing.AccessTokenLifetimeMinutes = model.AccessTokenLifetimeMinutes.Value;
        }

        if (model.RefreshTokenLifetimeDays.HasValue)
        {
            existing.RefreshTokenLifetimeDays = model.RefreshTokenLifetimeDays.Value;
        }

        if (!created)
        {
            _db.AuthSettings.Update(existing);
        }

        if (!string.Equals(previousMode, "OAuth", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(normalizedMode, "OAuth", StringComparison.OrdinalIgnoreCase))
        {
            // Switching to OAuth: clear any stale tokens to start from a clean state.
            var tokens = await _db.AuthTokens.ToListAsync(cancellationToken);
            if (tokens.Count > 0)
            {
                _db.AuthTokens.RemoveRange(tokens);
            }
        }

        if (string.Equals(previousMode, "OAuth", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedMode, "OAuth", StringComparison.OrdinalIgnoreCase))
        {
            // Switching away from OAuth: revoke all tokens.
            var tokens = await _db.AuthTokens.ToListAsync(cancellationToken);
            if (tokens.Count > 0)
            {
                _db.AuthTokens.RemoveRange(tokens);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _authSettingsService.InvalidateCacheAsync();

        var hasTokens = await _db.AuthTokens
            .AsNoTracking()
            .AnyAsync(cancellationToken);

        var viewModel = new AuthSettingsViewModel
        {
            Mode = existing.Mode,
            BearerToken = existing.BearerToken,
            OAuthClientId = existing.OAuthClientId,
            OAuthClientSecret = existing.OAuthClientSecret,
            AccessTokenLifetimeMinutes = existing.AccessTokenLifetimeMinutes,
            RefreshTokenLifetimeDays = existing.RefreshTokenLifetimeDays,
            HasAnyTokens = hasTokens
        };

        return Ok(viewModel);
    }

    private bool IsAdminRequest()
    {
        var requiredKey = Environment.GetEnvironmentVariable("AUTH_SETTINGS_ADMIN_KEY");
        if (string.IsNullOrWhiteSpace(requiredKey))
        {
            // No key configured: treat all authenticated callers as admin (dev convenience).
            return true;
        }

        if (!Request.Headers.TryGetValue("X-Admin-Key", out var headerValues))
        {
            return false;
        }

        var provided = headerValues.ToString();
        return string.Equals(provided, requiredKey, StringComparison.Ordinal);
    }
}

