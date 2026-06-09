using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.System;
using MockHealthSystem.Infrastructure.Data;

namespace MockHealthSystem.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public class SystemController(AppDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Get a list of Conditions. This endpoint implements a minimal OData-like interface.
    /// </summary>
    /// <param name="queryOptions">
    /// Optional OData query options header for compatibility with Clinical Conductor.
    /// Currently only simple paging via <paramref name="skip"/> and <paramref name="top"/> is honored.
    /// </param>
    /// <param name="skip">Number of items to skip (default 0).</param>
    /// <param name="top">Maximum number of items to return (default 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("conditions/odata")]
    public async Task<ActionResult<ODataPageResult<SysConditionViewModel>>> GetConditionsOdata(
        [FromHeader(Name = "queryOptions")] string? queryOptions,
        [FromQuery] int? skip,
        [FromQuery] int? top,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Conditions.AsNoTracking().OrderBy(c => c.Id);

        var totalCount = await query.LongCountAsync(cancellationToken);

        var safeSkip = skip.GetValueOrDefault(0);
        if (safeSkip < 0)
        {
            safeSkip = 0;
        }

        var safeTop = top.GetValueOrDefault(100);
        if (safeTop <= 0)
        {
            safeTop = 100;
        }

        var items = await query
            .Skip(safeSkip)
            .Take(safeTop)
            .Select(c => SysConditionViewModel.FromEntity(c))
            .ToListAsync(cancellationToken);

        var result = new ODataPageResult<SysConditionViewModel>
        {
            Items = items,
            Count = totalCount,
            NextPageLink = null
        };

        return Ok(result);
    }

    /// <summary>
    /// Get a list of Medications. This endpoint implements a minimal OData-like interface.
    /// </summary>
    /// <param name="queryOptions">
    /// Optional OData query options header for compatibility with Clinical Conductor.
    /// Currently only simple paging via <paramref name="skip"/> and <paramref name="top"/> is honored.
    /// </param>
    /// <param name="skip">Number of items to skip (default 0).</param>
    /// <param name="top">Maximum number of items to return (default 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("medications/odata")]
    public async Task<ActionResult<ODataPageResult<SysMedicationViewModel>>> GetMedicationsOdata(
        [FromHeader(Name = "queryOptions")] string? queryOptions,
        [FromQuery] int? skip,
        [FromQuery] int? top,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Medications.AsNoTracking().OrderBy(m => m.Id);

        var totalCount = await query.LongCountAsync(cancellationToken);

        var safeSkip = skip.GetValueOrDefault(0);
        if (safeSkip < 0)
        {
            safeSkip = 0;
        }

        var safeTop = top.GetValueOrDefault(100);
        if (safeTop <= 0)
        {
            safeTop = 100;
        }

        var items = await query
            .Skip(safeSkip)
            .Take(safeTop)
            .Select(m => SysMedicationViewModel.FromEntity(m))
            .ToListAsync(cancellationToken);

        var result = new ODataPageResult<SysMedicationViewModel>
        {
            Items = items,
            Count = totalCount,
            NextPageLink = null
        };

        return Ok(result);
    }

    /// <summary>
    /// Get a list of Allergies. This endpoint implements a minimal OData-like interface.
    /// </summary>
    /// <param name="queryOptions">
    /// Optional OData query options header for compatibility with Clinical Conductor.
    /// Currently only simple paging via <paramref name="skip"/> and <paramref name="top"/> is honored.
    /// </param>
    /// <param name="skip">Number of items to skip (default 0).</param>
    /// <param name="top">Maximum number of items to return (default 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("allergies/odata")]
    public async Task<ActionResult<ODataPageResult<SysAllergyViewModel>>> GetAllergiesOdata(
        [FromHeader(Name = "queryOptions")] string? queryOptions,
        [FromQuery] int? skip,
        [FromQuery] int? top,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Allergies.AsNoTracking().OrderBy(a => a.Id);

        var totalCount = await query.LongCountAsync(cancellationToken);

        var safeSkip = skip.GetValueOrDefault(0);
        if (safeSkip < 0)
        {
            safeSkip = 0;
        }

        var safeTop = top.GetValueOrDefault(100);
        if (safeTop <= 0)
        {
            safeTop = 100;
        }

        var items = await query
            .Skip(safeSkip)
            .Take(safeTop)
            .Select(a => SysAllergyViewModel.FromEntity(a))
            .ToListAsync(cancellationToken);

        var result = new ODataPageResult<SysAllergyViewModel>
        {
            Items = items,
            Count = totalCount,
            NextPageLink = null
        };

        return Ok(result);
    }
}

