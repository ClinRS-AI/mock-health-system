using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Studies;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>Protocol Version endpoints scoped to a parent study.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/studies/{studyId:int}/protocol-versions")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class ProtocolVersionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProtocolVersionsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>List protocol versions for a study.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProtocolVersionViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProtocolVersions(int studyId, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        var list = await _db.ProtocolVersions.Include(pv => pv.Study).Where(pv => pv.StudyId == studyId).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get a protocol version by ID, scoped to its parent study.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProtocolVersionViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProtocolVersion(int studyId, int id, CancellationToken cancellationToken)
    {
        var pv = await _db.ProtocolVersions.Include(x => x.Study).FirstOrDefaultAsync(x => x.Id == id && x.StudyId == studyId, cancellationToken);
        if (pv == null) return NotFound();
        return Ok(StudyMappingService.ToViewModel(pv));
    }

    /// <summary>Create a protocol version scoped to the study.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProtocolVersionViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProtocolVersion(int studyId, [FromBody] ProtocolVersionEditModel editModel, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();

        var pv = new ProtocolVersion { Uid = Guid.NewGuid(), StudyId = studyId };
        StudyMappingService.ApplyEditModel(pv, editModel);
        _db.ProtocolVersions.Add(pv);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.ProtocolVersions.Include(x => x.Study).FirstAsync(x => x.Id == pv.Id, cancellationToken);
        return CreatedAtAction(nameof(GetProtocolVersion), new { studyId, id = pv.Id, version = "1.0" }, StudyMappingService.ToViewModel(created));
    }

    /// <summary>Update a protocol version, scoped to its parent study.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProtocolVersionViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProtocolVersion(int studyId, int id, [FromBody] ProtocolVersionEditModel editModel, CancellationToken cancellationToken)
    {
        var pv = await _db.ProtocolVersions.Include(x => x.Study).FirstOrDefaultAsync(x => x.Id == id && x.StudyId == studyId, cancellationToken);
        if (pv == null) return NotFound();

        StudyMappingService.ApplyEditModel(pv, editModel);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(StudyMappingService.ToViewModel(pv));
    }

    /// <summary>Delete a protocol version, scoped to its parent study. 409 if still referenced by an Arm/Visit.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteProtocolVersion(int studyId, int id, CancellationToken cancellationToken)
    {
        var pv = await _db.ProtocolVersions.FirstOrDefaultAsync(x => x.Id == id && x.StudyId == studyId, cancellationToken);
        if (pv == null) return NotFound();

        var referenced = await _db.StudyArms.AnyAsync(a => a.ProtocolVersionId == id, cancellationToken)
            || await _db.StudyVisits.AnyAsync(v => v.ProtocolVersionId == id, cancellationToken);
        if (referenced) return Conflict("Protocol version is still referenced by one or more arms or visits.");

        _db.ProtocolVersions.Remove(pv);
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Conflict("Protocol version is still referenced by one or more arms or visits."); }
        return NoContent();
    }
}
