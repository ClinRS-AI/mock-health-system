using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Auth;
using MockHealthSystem.Api.RateLimiting;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Api.Services.AdminSession;
using MockHealthSystem.Api.Swagger;
using MockHealthSystem.Infrastructure.Data;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// Administration endpoints for managing authentication settings.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth-settings")]
[AllowAnonymous]
[RequiresAdminAuth]
public sealed class AuthSettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthSettingsService _authSettingsService;
    private readonly IAdminRequestValidator _adminRequestValidator;
    private readonly IRateLimitCounterStore _rateLimitCounterStore;

    public AuthSettingsController(
        AppDbContext db,
        IAuthSettingsService authSettingsService,
        IAdminRequestValidator adminRequestValidator,
        IRateLimitCounterStore rateLimitCounterStore)
    {
        _db = db;
        _authSettingsService = authSettingsService;
        _adminRequestValidator = adminRequestValidator;
        _rateLimitCounterStore = rateLimitCounterStore;
    }

    /// <summary>
    /// Returns the current authentication settings.
    /// </summary>
    /// <returns>Authentication settings and summary info.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AuthSettingsViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: false))
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
            HasAnyTokens = hasTokens,
            RateLimitEnabled = settings.RateLimitEnabled,
            RateLimitPerSecond = settings.RateLimitPerSecond,
            RateLimitPerMinute = settings.RateLimitPerMinute
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
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: false))
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

        if (model.RateLimitEnabled.HasValue)
        {
            existing.RateLimitEnabled = model.RateLimitEnabled.Value;
        }

        if (model.RateLimitPerSecond.HasValue)
        {
            existing.RateLimitPerSecond = model.RateLimitPerSecond.Value;
        }

        if (model.RateLimitPerMinute.HasValue)
        {
            existing.RateLimitPerMinute = model.RateLimitPerMinute.Value;
        }

        // Validate limits when rate limiting would be active after this save
        if (existing.RateLimitEnabled)
        {
            if (existing.RateLimitPerSecond < 1)
                return BadRequest("RateLimitPerSecond must be at least 1 when rate limiting is enabled.");
            if (existing.RateLimitPerMinute < 1)
                return BadRequest("RateLimitPerMinute must be at least 1 when rate limiting is enabled.");
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
        _rateLimitCounterStore.ResetAll();

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
            HasAnyTokens = hasTokens,
            RateLimitEnabled = existing.RateLimitEnabled,
            RateLimitPerSecond = existing.RateLimitPerSecond,
            RateLimitPerMinute = existing.RateLimitPerMinute
        };

        return Ok(viewModel);
    }
}

