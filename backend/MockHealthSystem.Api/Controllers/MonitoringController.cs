using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Monitoring;
using MockHealthSystem.Api.Models.Monitoring;
using MockHealthSystem.Api.Services.AdminSession;
using MockHealthSystem.Infrastructure.Data;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// Monitoring endpoints for inspecting recent external API requests.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/monitoring")]
[AllowAnonymous]
public sealed class MonitoringController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAdminRequestValidator _adminRequestValidator;

    public MonitoringController(AppDbContext db, IAdminRequestValidator adminRequestValidator)
    {
        _db = db;
        _adminRequestValidator = adminRequestValidator;
    }

    /// <summary>
    /// Returns a list of recent API requests for monitoring purposes.
    /// </summary>
    /// <param name="take">Maximum number of records to return (default 100, max 500).</param>
    /// <param name="pathPrefix">Optional path prefix to filter on.</param>
    /// <param name="statusCode">Optional exact status code to filter on.</param>
    /// <param name="sinceUtc">Optional lower bound on CreatedAtUtc (UTC).</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(IEnumerable<ApiRequestLogSummaryModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequestsAsync(
        [FromQuery] int? take,
        [FromQuery] string? pathPrefix,
        [FromQuery] int? statusCode,
        [FromQuery] DateTime? sinceUtc,
        CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: false))
        {
            return Forbid();
        }

        var limit = MonitoringRequestListLimits.ClampTake(take);

        var query = _db.ApiRequestLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(pathPrefix))
        {
            query = query.Where(x => x.Path.StartsWith(pathPrefix));
        }

        if (statusCode.HasValue)
        {
            query = query.Where(x => x.StatusCode == statusCode.Value);
        }

        if (sinceUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= sinceUtc.Value);
        }

        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var results = rows.Select(x => new ApiRequestLogSummaryModel
        {
            Id = x.Id,
            CreatedAtUtc = x.CreatedAtUtc,
            Method = x.Method,
            Path = x.Path,
            StatusCode = x.StatusCode,
            DurationMs = x.DurationMs,
            Origin = x.Origin
        });

        return Ok(results);
    }

    /// <summary>
    /// Returns detailed information for a specific API request.
    /// </summary>
    [HttpGet("requests/{id:int}")]
    [ProducesResponseType(typeof(ApiRequestLogDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequestAsync(int id, CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: false))
        {
            return Forbid();
        }

        var entity = await _db.ApiRequestLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var model = new ApiRequestLogDetailModel
        {
            Id = entity.Id,
            CreatedAtUtc = entity.CreatedAtUtc,
            Method = entity.Method,
            Path = entity.Path,
            QueryString = entity.QueryString,
            StatusCode = entity.StatusCode,
            DurationMs = entity.DurationMs,
            Origin = entity.Origin,
            Referer = entity.Referer,
            UserAgent = entity.UserAgent,
            RemoteIp = entity.RemoteIp,
            RequestBody = entity.RequestBody,
            ResponseBody = entity.ResponseBody,
            CorrelationId = entity.CorrelationId
        };

        return Ok(model);
    }

    private const int StatsMaxRequests = 200;

    /// <summary>
    /// Returns aggregated stats for the last 200 requests (status breakdown and duration stats) for dashboard visualizations.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(MonitoringStatsModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatsAsync(CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: false))
        {
            return Forbid();
        }

        var rows = await _db.ApiRequestLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(StatsMaxRequests)
            .Select(x => new { x.StatusCode, x.DurationMs })
            .ToListAsync(cancellationToken);

        var statusBreakdown = rows
            .GroupBy(x => x.StatusCode)
            .OrderBy(g => g.Key)
            .Select(g => new StatusBreakdownItem { StatusCode = g.Key, Count = g.Count() })
            .ToList();

        int? maxDurationMs = null;
        double? averageDurationMs = null;
        double? percentile95Ms = null;

        if (rows.Count > 0)
        {
            var durations = rows.Select(x => x.DurationMs).ToList();
            maxDurationMs = durations.Max();
            averageDurationMs = durations.Average();

            var sorted = durations.OrderBy(d => d).ToList();
            var idx = (int)Math.Ceiling(0.95 * sorted.Count) - 1;
            if (idx < 0) idx = 0;
            if (idx >= sorted.Count) idx = sorted.Count - 1;
            percentile95Ms = Math.Round((double)sorted[idx], 1);
        }

        var model = new MonitoringStatsModel
        {
            StatusBreakdown = statusBreakdown,
            RequestCount = rows.Count,
            AverageDurationMs = averageDurationMs,
            Percentile95DurationMs = percentile95Ms,
            MaxDurationMs = maxDurationMs
        };

        return Ok(model);
    }
}

