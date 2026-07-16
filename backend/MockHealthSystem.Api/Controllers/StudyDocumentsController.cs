using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Studies;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>Study Document endpoints scoped to a parent study.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/studies/{studyId:int}/documents")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class StudyDocumentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudyDocumentsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>List documents for a study.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StudyDocumentViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocuments(int studyId, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        var list = await _db.StudyDocuments.Where(d => d.StudyId == studyId).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get list of documents for a study (OData-style endpoint; simple list without query options).</summary>
    [HttpGet("odata")]
    [ProducesResponseType(typeof(IEnumerable<StudyDocumentViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentsOData(int studyId, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        var list = await _db.StudyDocuments.Where(d => d.StudyId == studyId).OrderBy(d => d.Id).Take(100).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get a document by ID, scoped to its parent study.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudyDocumentViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(int studyId, int id, CancellationToken cancellationToken)
    {
        var document = await _db.StudyDocuments.FirstOrDefaultAsync(d => d.Id == id && d.StudyId == studyId, cancellationToken);
        if (document == null) return NotFound();
        return Ok(StudyMappingService.ToViewModel(document));
    }

    /// <summary>Status change history for a document, ordered by ChangedOn descending.</summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(IEnumerable<StudyDocumentStatusHistoryViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentHistory(int studyId, int id, CancellationToken cancellationToken)
    {
        var document = await _db.StudyDocuments.FirstOrDefaultAsync(d => d.Id == id && d.StudyId == studyId, cancellationToken);
        if (document == null) return NotFound();

        var history = await _db.StudyDocumentStatusHistories
            .Include(h => h.ChangedByStaff)
            .Where(h => h.StudyDocumentId == id)
            .OrderByDescending(h => h.ChangedOn)
            .ToListAsync(cancellationToken);
        return Ok(history.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Create a document scoped to the study. Also creates the initial status history row.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StudyDocumentViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDocument(int studyId, [FromBody] StudyDocumentEditModel editModel, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();

        var document = new StudyDocument { Uid = Guid.NewGuid(), StudyId = studyId };
        StudyMappingService.ApplyEditModel(document, editModel);
        _db.StudyDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        _db.StudyDocumentStatusHistories.Add(new StudyDocumentStatusHistory
        {
            StudyDocumentId = document.Id,
            StatusName = document.StatusName,
            ChangedOn = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetDocument), new { studyId, id = document.Id, version = "1.0" }, StudyMappingService.ToViewModel(document));
    }

    /// <summary>Update a document, scoped to its parent study. A status change appends a new history row.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StudyDocumentViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDocument(int studyId, int id, [FromBody] StudyDocumentEditModel editModel, CancellationToken cancellationToken)
    {
        var document = await _db.StudyDocuments.FirstOrDefaultAsync(d => d.Id == id && d.StudyId == studyId, cancellationToken);
        if (document == null) return NotFound();

        var statusChanged = document.StatusName != editModel.StatusName;
        StudyMappingService.ApplyEditModel(document, editModel);

        if (statusChanged)
        {
            _db.StudyDocumentStatusHistories.Add(new StudyDocumentStatusHistory
            {
                StudyDocumentId = document.Id,
                StatusName = document.StatusName,
                ChangedOn = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(StudyMappingService.ToViewModel(document));
    }

    /// <summary>Delete a document, scoped to its parent study.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(int studyId, int id, CancellationToken cancellationToken)
    {
        var document = await _db.StudyDocuments.FirstOrDefaultAsync(d => d.Id == id && d.StudyId == studyId, cancellationToken);
        if (document == null) return NotFound();
        _db.StudyDocuments.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
