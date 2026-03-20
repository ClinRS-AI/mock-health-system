using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// Health and readiness checks for the API.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Returns a simple health status message indicating the API is running.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health status message.</returns>
    /// <response code="200">API is running.</response>
    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IActionResult>(Ok("Mock Health System API is running."));
    }
}
