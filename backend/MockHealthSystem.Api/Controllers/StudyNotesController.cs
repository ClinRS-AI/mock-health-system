using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Studies;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>Study Note endpoints scoped to a parent study.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/studies/{studyId:int}/notes")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class StudyNotesController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudyNotesController(AppDbContext db)
    {
        _db = db;
    }

    private IQueryable<StudyNote> IncludeAll(IQueryable<StudyNote> query) => query
        .Include(n => n.Staff)
        .Include(n => n.LastUpdatedStaff);

    /// <summary>Uses tracking FindAsync deliberately: it lets EF's automatic relationship-fixup
    /// resolve the Staff/LastUpdatedStaff navigations on SaveChangesAsync without a manual re-query.</summary>
    private async Task<string?> ValidateStaffReferencesAsync(int? staffId, int? lastUpdatedStaffId, CancellationToken cancellationToken)
    {
        if (staffId.HasValue && await _db.Staff.FindAsync([staffId.Value], cancellationToken) == null)
            return "StaffId does not reference an existing staff member.";
        if (lastUpdatedStaffId.HasValue && await _db.Staff.FindAsync([lastUpdatedStaffId.Value], cancellationToken) == null)
            return "LastUpdatedStaffId does not reference an existing staff member.";
        return null;
    }

    /// <summary>List notes for a study.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StudyNoteViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotes(int studyId, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        var list = await IncludeAll(_db.StudyNotes.AsQueryable()).Where(n => n.StudyId == studyId).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get list of notes for a study (OData-style endpoint; simple list without query options).</summary>
    [HttpGet("odata")]
    [ProducesResponseType(typeof(IEnumerable<StudyNoteViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotesOData(int studyId, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        var list = await IncludeAll(_db.StudyNotes.AsQueryable()).Where(n => n.StudyId == studyId).OrderBy(n => n.Id).Take(100).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get a note by ID, scoped to its parent study.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudyNoteViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNote(int studyId, int id, CancellationToken cancellationToken)
    {
        var note = await IncludeAll(_db.StudyNotes.AsQueryable()).FirstOrDefaultAsync(n => n.Id == id && n.StudyId == studyId, cancellationToken);
        if (note == null) return NotFound();
        return Ok(StudyMappingService.ToViewModel(note));
    }

    /// <summary>Create a note scoped to the study.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StudyNoteViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateNote(int studyId, [FromBody] StudyNoteEditModel editModel, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();

        var staffError = await ValidateStaffReferencesAsync(editModel.StaffId, editModel.LastUpdatedStaffId, cancellationToken);
        if (staffError != null) return BadRequest(staffError);

        var note = new StudyNote { StudyId = studyId, NoteDate = editModel.Date ?? DateTime.UtcNow };
        StudyMappingService.ApplyEditModel(note, editModel);
        _db.StudyNotes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await IncludeAll(_db.StudyNotes.AsQueryable()).FirstAsync(n => n.Id == note.Id, cancellationToken);
        return CreatedAtAction(nameof(GetNote), new { studyId, id = note.Id, version = "1.0" }, StudyMappingService.ToViewModel(created));
    }

    /// <summary>Update a note, scoped to its parent study. 409 if the note is locked.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StudyNoteViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateNote(int studyId, int id, [FromBody] StudyNoteEditModel editModel, CancellationToken cancellationToken)
    {
        var note = await IncludeAll(_db.StudyNotes.AsQueryable()).FirstOrDefaultAsync(n => n.Id == id && n.StudyId == studyId, cancellationToken);
        if (note == null) return NotFound();
        if (note.Locked) return Conflict("Note is locked and cannot be updated.");

        var staffError = await ValidateStaffReferencesAsync(editModel.StaffId, editModel.LastUpdatedStaffId, cancellationToken);
        if (staffError != null) return BadRequest(staffError);

        StudyMappingService.ApplyEditModel(note, editModel);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(StudyMappingService.ToViewModel(note));
    }

    /// <summary>Delete a note, scoped to its parent study. 409 if the note is locked.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteNote(int studyId, int id, CancellationToken cancellationToken)
    {
        var note = await _db.StudyNotes.FirstOrDefaultAsync(n => n.Id == id && n.StudyId == studyId, cancellationToken);
        if (note == null) return NotFound();
        if (note.Locked) return Conflict("Note is locked and cannot be deleted.");
        _db.StudyNotes.Remove(note);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
