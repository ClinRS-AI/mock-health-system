using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Studies;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>Study Role endpoints scoped to a parent study.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/studies/{studyId:int}/roles")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class StudyRolesController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudyRolesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>List roles for a study, including assigned staff.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StudyRoleViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoles(int studyId, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        var list = await _db.StudyRoles.Include(r => r.RoleStaff).ThenInclude(rs => rs.Staff)
            .Where(r => r.StudyId == studyId).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get a role by ID, scoped to its parent study.</summary>
    [HttpGet("{roleId:int}")]
    [ProducesResponseType(typeof(StudyRoleViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(int studyId, int roleId, CancellationToken cancellationToken)
    {
        var role = await _db.StudyRoles.Include(r => r.RoleStaff).ThenInclude(rs => rs.Staff)
            .FirstOrDefaultAsync(r => r.Id == roleId && r.StudyId == studyId, cancellationToken);
        if (role == null) return NotFound();
        return Ok(StudyMappingService.ToViewModel(role));
    }

    /// <summary>Replace the assigned-staff set for the role.</summary>
    [HttpPut("{roleId:int}")]
    [ProducesResponseType(typeof(StudyRoleViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoleStaff(int studyId, int roleId, [FromBody] IList<StudyRoleStaffEditModel> staffAssignments, CancellationToken cancellationToken)
    {
        var role = await _db.StudyRoles.Include(r => r.RoleStaff).ThenInclude(rs => rs.Staff)
            .FirstOrDefaultAsync(r => r.Id == roleId && r.StudyId == studyId, cancellationToken);
        if (role == null) return NotFound();

        var staffIds = staffAssignments.Select(a => a.StaffId).ToList();
        if (staffIds.Count != staffIds.Distinct().Count())
            return BadRequest("staffAssignments contains duplicate staffId values.");

        var existingStaffIds = await _db.Staff.Where(s => staffIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(cancellationToken);
        if (existingStaffIds.Count != staffIds.Count)
            return BadRequest("One or more staffId values do not reference an existing staff member.");

        role.RoleStaff.Clear();
        foreach (var assignment in staffAssignments)
            role.RoleStaff.Add(new StudyRoleStaff { StudyRoleId = role.Id, StaffId = assignment.StaffId, Priority = assignment.Priority });

        await _db.SaveChangesAsync(cancellationToken);

        var updated = await _db.StudyRoles.Include(r => r.RoleStaff).ThenInclude(rs => rs.Staff).FirstAsync(r => r.Id == roleId, cancellationToken);
        return Ok(StudyMappingService.ToViewModel(updated));
    }
}
