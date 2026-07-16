using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Studies;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>Study Visit endpoints scoped to a parent study.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/studies/{studyId:int}/visits")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class StudyVisitsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudyVisitsController(AppDbContext db)
    {
        _db = db;
    }

    private IQueryable<StudyVisit> IncludeAll(IQueryable<StudyVisit> query) => query
        .Include(v => v.Study)
        .Include(v => v.ProtocolVersion)
        .Include(v => v.VisitArms).ThenInclude(va => va.StudyArm);

    /// <summary>Uses a tracking FindAsync deliberately: it lets EF's automatic relationship-fixup
    /// resolve the ProtocolVersion navigation on SaveChangesAsync without a manual re-query.</summary>
    private async Task<string?> ValidateProtocolVersionAsync(int? protocolVersionId, CancellationToken cancellationToken)
    {
        if (protocolVersionId.HasValue && await _db.ProtocolVersions.FindAsync([protocolVersionId.Value], cancellationToken) == null)
            return "ProtocolVersionId does not reference an existing protocol version.";
        return null;
    }

    /// <summary>Get list of visits for a study (OData-style endpoint; simple list without query options).</summary>
    [HttpGet("odata")]
    [ProducesResponseType(typeof(IEnumerable<StudyVisitViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVisitsOData(int studyId, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        var list = await IncludeAll(_db.StudyVisits.AsQueryable()).Where(v => v.StudyId == studyId).OrderBy(v => v.Id).Take(100).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get a visit by ID, scoped to its parent study.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudyVisitViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVisit(int studyId, int id, CancellationToken cancellationToken)
    {
        var visit = await IncludeAll(_db.StudyVisits.AsQueryable()).FirstOrDefaultAsync(v => v.Id == id && v.StudyId == studyId, cancellationToken);
        if (visit == null) return NotFound();
        return Ok(StudyMappingService.ToViewModel(visit));
    }

    /// <summary>Arms associated with the visit.</summary>
    [HttpGet("{visitId:int}/arms")]
    [ProducesResponseType(typeof(IEnumerable<StudyArmViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVisitArms(int studyId, int visitId, CancellationToken cancellationToken)
    {
        var visit = await _db.StudyVisits.FirstOrDefaultAsync(v => v.Id == visitId && v.StudyId == studyId, cancellationToken);
        if (visit == null) return NotFound();

        var arms = await _db.StudyVisitArms
            .Where(va => va.VisitId == visitId)
            .Include(va => va.StudyArm).ThenInclude(a => a.Study)
            .Include(va => va.StudyArm).ThenInclude(a => a.ProtocolVersion)
            .Select(va => va.StudyArm)
            .ToListAsync(cancellationToken);
        return Ok(arms.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Create a visit scoped to the study.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StudyVisitViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateVisit(int studyId, [FromBody] StudyVisitEditModel editModel, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();

        var protocolVersionError = await ValidateProtocolVersionAsync(editModel.ProtocolVersionId, cancellationToken);
        if (protocolVersionError != null) return BadRequest(protocolVersionError);

        var visit = new StudyVisit { Uid = Guid.NewGuid(), StudyId = studyId };
        StudyMappingService.ApplyEditModel(visit, editModel);
        _db.StudyVisits.Add(visit);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await IncludeAll(_db.StudyVisits.AsQueryable()).FirstAsync(v => v.Id == visit.Id, cancellationToken);
        return CreatedAtAction(nameof(GetVisit), new { studyId, id = visit.Id, version = "1.0" }, StudyMappingService.ToViewModel(created));
    }

    /// <summary>Update a visit, scoped to its parent study.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StudyVisitViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVisit(int studyId, int id, [FromBody] StudyVisitEditModel editModel, CancellationToken cancellationToken)
    {
        var visit = await IncludeAll(_db.StudyVisits.AsQueryable()).FirstOrDefaultAsync(v => v.Id == id && v.StudyId == studyId, cancellationToken);
        if (visit == null) return NotFound();

        var protocolVersionError = await ValidateProtocolVersionAsync(editModel.ProtocolVersionId, cancellationToken);
        if (protocolVersionError != null) return BadRequest(protocolVersionError);

        StudyMappingService.ApplyEditModel(visit, editModel);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(StudyMappingService.ToViewModel(visit));
    }

    /// <summary>Delete a visit, scoped to its parent study.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVisit(int studyId, int id, CancellationToken cancellationToken)
    {
        var visit = await _db.StudyVisits.FirstOrDefaultAsync(v => v.Id == id && v.StudyId == studyId, cancellationToken);
        if (visit == null) return NotFound();
        _db.StudyVisits.Remove(visit);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
