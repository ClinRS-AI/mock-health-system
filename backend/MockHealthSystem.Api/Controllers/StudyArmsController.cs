using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Studies;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>Study Arm endpoints scoped to a parent study.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/studies/{studyId:int}/arms")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class StudyArmsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudyArmsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Uses a tracking FindAsync deliberately: it lets EF's automatic relationship-fixup
    /// resolve the ProtocolVersion navigation on SaveChangesAsync without a manual re-query.</summary>
    private async Task<string?> ValidateProtocolVersionAsync(int? protocolVersionId, CancellationToken cancellationToken)
    {
        if (protocolVersionId.HasValue && await _db.ProtocolVersions.FindAsync([protocolVersionId.Value], cancellationToken) == null)
            return "ProtocolVersionId does not reference an existing protocol version.";
        return null;
    }

    /// <summary>List arms for a study.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StudyArmViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArms(int studyId, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        var list = await _db.StudyArms.Include(a => a.Study).Include(a => a.ProtocolVersion)
            .Where(a => a.StudyId == studyId).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get an arm by ID, scoped to its parent study.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudyArmViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArm(int studyId, int id, CancellationToken cancellationToken)
    {
        var arm = await _db.StudyArms.Include(a => a.Study).Include(a => a.ProtocolVersion)
            .FirstOrDefaultAsync(a => a.Id == id && a.StudyId == studyId, cancellationToken);
        if (arm == null) return NotFound();
        return Ok(StudyMappingService.ToViewModel(arm));
    }

    /// <summary>Visits associated with the arm.</summary>
    [HttpGet("{armId:int}/visits")]
    [ProducesResponseType(typeof(IEnumerable<StudyVisitViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArmVisits(int studyId, int armId, CancellationToken cancellationToken)
    {
        var arm = await _db.StudyArms.FirstOrDefaultAsync(a => a.Id == armId && a.StudyId == studyId, cancellationToken);
        if (arm == null) return NotFound();

        var visits = await _db.StudyVisitArms
            .Where(va => va.ArmId == armId)
            .Include(va => va.StudyVisit).ThenInclude(v => v.Study)
            .Include(va => va.StudyVisit).ThenInclude(v => v.ProtocolVersion)
            .Include(va => va.StudyVisit).ThenInclude(v => v.VisitArms).ThenInclude(va2 => va2.StudyArm)
            .Select(va => va.StudyVisit)
            .ToListAsync(cancellationToken);
        return Ok(visits.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Create an arm scoped to the study.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StudyArmViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateArm(int studyId, [FromBody] StudyArmEditModel editModel, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();

        var protocolVersionError = await ValidateProtocolVersionAsync(editModel.ProtocolVersionId, cancellationToken);
        if (protocolVersionError != null) return BadRequest(protocolVersionError);

        var arm = new StudyArm { Uid = Guid.NewGuid(), StudyId = studyId };
        StudyMappingService.ApplyEditModel(arm, editModel);
        _db.StudyArms.Add(arm);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.StudyArms.Include(a => a.Study).Include(a => a.ProtocolVersion).FirstAsync(a => a.Id == arm.Id, cancellationToken);
        return CreatedAtAction(nameof(GetArm), new { studyId, id = arm.Id, version = "1.0" }, StudyMappingService.ToViewModel(created));
    }

    /// <summary>Update an arm, scoped to its parent study.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StudyArmViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateArm(int studyId, int id, [FromBody] StudyArmEditModel editModel, CancellationToken cancellationToken)
    {
        var arm = await _db.StudyArms.Include(a => a.Study).Include(a => a.ProtocolVersion)
            .FirstOrDefaultAsync(a => a.Id == id && a.StudyId == studyId, cancellationToken);
        if (arm == null) return NotFound();

        var protocolVersionError = await ValidateProtocolVersionAsync(editModel.ProtocolVersionId, cancellationToken);
        if (protocolVersionError != null) return BadRequest(protocolVersionError);

        StudyMappingService.ApplyEditModel(arm, editModel);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(StudyMappingService.ToViewModel(arm));
    }

    /// <summary>Delete an arm, scoped to its parent study.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteArm(int studyId, int id, CancellationToken cancellationToken)
    {
        var arm = await _db.StudyArms.FirstOrDefaultAsync(a => a.Id == id && a.StudyId == studyId, cancellationToken);
        if (arm == null) return NotFound();
        _db.StudyArms.Remove(arm);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Associate a visit with the arm. 400 if the visit doesn't belong to the same study.</summary>
    [HttpPost("{armId:int}/visits/{visitId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddArmVisit(int studyId, int armId, int visitId, CancellationToken cancellationToken)
    {
        var arm = await _db.StudyArms.FirstOrDefaultAsync(a => a.Id == armId && a.StudyId == studyId, cancellationToken);
        if (arm == null) return NotFound();
        var visit = await _db.StudyVisits.FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken);
        if (visit == null) return NotFound();
        if (visit.StudyId != studyId) return BadRequest("Visit does not belong to the same study as the arm.");

        var exists = await _db.StudyVisitArms.AnyAsync(x => x.ArmId == armId && x.VisitId == visitId, cancellationToken);
        if (!exists)
        {
            _db.StudyVisitArms.Add(new StudyVisitArm { ArmId = armId, VisitId = visitId });
            await _db.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    /// <summary>Remove a visit association from the arm.</summary>
    [HttpDelete("{armId:int}/visits/{visitId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveArmVisit(int studyId, int armId, int visitId, CancellationToken cancellationToken)
    {
        var arm = await _db.StudyArms.FirstOrDefaultAsync(a => a.Id == armId && a.StudyId == studyId, cancellationToken);
        if (arm == null) return NotFound();
        var link = await _db.StudyVisitArms.FirstOrDefaultAsync(x => x.ArmId == armId && x.VisitId == visitId, cancellationToken);
        if (link == null) return NotFound();
        _db.StudyVisitArms.Remove(link);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
