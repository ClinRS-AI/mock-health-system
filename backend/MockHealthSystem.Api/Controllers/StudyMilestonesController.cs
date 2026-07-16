using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Studies;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>Study Milestone endpoints scoped to a parent study.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/studies/{studyId:int}/milestones")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class StudyMilestonesController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudyMilestonesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Uses a tracking FindAsync deliberately: it lets EF's automatic relationship-fixup
    /// resolve the AssignedToStaff navigation on SaveChangesAsync without a manual re-query.</summary>
    private async Task<string?> ValidateAssignedToStaffAsync(int? assignedToStaffId, CancellationToken cancellationToken)
    {
        if (assignedToStaffId.HasValue && await _db.Staff.FindAsync([assignedToStaffId.Value], cancellationToken) == null)
            return "AssignedToStaffId does not reference an existing staff member.";
        return null;
    }

    /// <summary>List milestones for a study.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StudyMilestoneViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMilestones(int studyId, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        var list = await _db.StudyMilestones.Include(m => m.AssignedToStaff).Where(m => m.StudyId == studyId).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get list of milestones for a study (OData-style endpoint; simple list without query options).</summary>
    [HttpGet("odata")]
    [ProducesResponseType(typeof(IEnumerable<StudyMilestoneViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMilestonesOData(int studyId, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        var list = await _db.StudyMilestones.Include(m => m.AssignedToStaff).Where(m => m.StudyId == studyId).OrderBy(m => m.Id).Take(100).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get a milestone by ID, scoped to its parent study.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudyMilestoneViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMilestone(int studyId, int id, CancellationToken cancellationToken)
    {
        var milestone = await _db.StudyMilestones.Include(m => m.AssignedToStaff)
            .FirstOrDefaultAsync(m => m.Id == id && m.StudyId == studyId, cancellationToken);
        if (milestone == null) return NotFound();
        return Ok(StudyMappingService.ToViewModel(milestone));
    }

    /// <summary>Create a milestone scoped to the study.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StudyMilestoneViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMilestone(int studyId, [FromBody] StudyMilestoneEditModel editModel, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();

        var assignedToStaffError = await ValidateAssignedToStaffAsync(editModel.AssignedToStaffId, cancellationToken);
        if (assignedToStaffError != null) return BadRequest(assignedToStaffError);

        var milestone = new StudyMilestone { StudyId = studyId };
        StudyMappingService.ApplyEditModel(milestone, editModel);
        _db.StudyMilestones.Add(milestone);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.StudyMilestones.Include(m => m.AssignedToStaff).FirstAsync(m => m.Id == milestone.Id, cancellationToken);
        return CreatedAtAction(nameof(GetMilestone), new { studyId, id = milestone.Id, version = "1.0" }, StudyMappingService.ToViewModel(created));
    }

    /// <summary>Update a milestone, scoped to its parent study.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StudyMilestoneViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMilestone(int studyId, int id, [FromBody] StudyMilestoneEditModel editModel, CancellationToken cancellationToken)
    {
        var milestone = await _db.StudyMilestones.Include(m => m.AssignedToStaff)
            .FirstOrDefaultAsync(m => m.Id == id && m.StudyId == studyId, cancellationToken);
        if (milestone == null) return NotFound();

        var assignedToStaffError = await ValidateAssignedToStaffAsync(editModel.AssignedToStaffId, cancellationToken);
        if (assignedToStaffError != null) return BadRequest(assignedToStaffError);

        StudyMappingService.ApplyEditModel(milestone, editModel);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(StudyMappingService.ToViewModel(milestone));
    }

    /// <summary>Delete a milestone, scoped to its parent study.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMilestone(int studyId, int id, CancellationToken cancellationToken)
    {
        var milestone = await _db.StudyMilestones.FirstOrDefaultAsync(m => m.Id == id && m.StudyId == studyId, cancellationToken);
        if (milestone == null) return NotFound();
        _db.StudyMilestones.Remove(milestone);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
