using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// CC-mirrored, study-scoped Subject status-history endpoint. Unlike every other Study
/// sub-resource route in this codebase (keyed by the numeric Study Id), this one is keyed by the
/// Study's Uid — matching the real CC API's shape for this specific endpoint (research.md
/// Decision 2, feature 006-subject-domain).
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/studies/{studyUid:guid}/subject-statuses")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class SubjectStatusesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubjectStatusesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Status-change history for subjects enrolled in a study (OData-style endpoint;
    /// simple list without query options), ordered by ChangedOn descending.</summary>
    [HttpGet("odata")]
    [ProducesResponseType(typeof(IEnumerable<Models.Subjects.SubjectStatusViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubjectStatusesOData(Guid studyUid, CancellationToken cancellationToken)
    {
        var study = await _db.Studies.FirstOrDefaultAsync(s => s.Uid == studyUid, cancellationToken);
        if (study == null) return NotFound();

        var history = await _db.SubjectStatuses
            .Include(h => h.ChangedByStaff)
            .Where(h => h.Subject.StudyId == study.Id)
            .OrderByDescending(h => h.ChangedOn)
            .Take(100)
            .ToListAsync(cancellationToken);
        return Ok(history.Select(SubjectMappingService.ToViewModel));
    }
}
