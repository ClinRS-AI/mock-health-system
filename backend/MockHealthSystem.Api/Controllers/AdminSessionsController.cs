using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockHealthSystem.Api.Models.Admin;
using MockHealthSystem.Api.Services.AdminSession;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// Mints short-lived JWTs for the admin UI after verifying AUTH_SETTINGS_ADMIN_KEY.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/sessions")]
[AllowAnonymous]
public sealed class AdminSessionsController : ControllerBase
{
    private readonly IAdminSessionJwtService _adminSessionJwt;

    public AdminSessionsController(IAdminSessionJwtService adminSessionJwt)
    {
        _adminSessionJwt = adminSessionJwt;
    }

    /// <summary>
    /// Exchanges the configured admin key for a short-lived session token (send as X-Admin-Session on admin routes).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateAdminSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Create([FromBody] CreateAdminSessionRequest? request)
    {
        var requiredKey = Environment.GetEnvironmentVariable("AUTH_SETTINGS_ADMIN_KEY");
        if (string.IsNullOrWhiteSpace(requiredKey))
        {
            return BadRequest(
                "Admin session minting is disabled because AUTH_SETTINGS_ADMIN_KEY is not set on the server. " +
                "Admin API routes remain open without a key in this configuration.");
        }

        var provided = request?.AdminKey?.Trim() ?? "";
        if (!string.Equals(provided, requiredKey, StringComparison.Ordinal))
        {
            return Forbid();
        }

        var minted = _adminSessionJwt.CreateSessionToken();
        if (minted is null)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "Could not mint admin session token (signing key material unavailable).");
        }

        return Ok(new CreateAdminSessionResponse
        {
            AccessToken = minted.AccessToken,
            ExpiresAtUtc = minted.ExpiresAtUtc,
        });
    }
}
