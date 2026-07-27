using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Subjects;
using MockHealthSystem.Api.Models.System;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// Subject endpoints aligned with the Clinical Conductor API — the enrollment-episode record
/// linking one Patient to one Study.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subjects")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class SubjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubjectsController(AppDbContext db)
    {
        _db = db;
    }

    private IQueryable<Subject> IncludeAll(IQueryable<Subject> query) => query
        .Include(s => s.Patient)
        .Include(s => s.Study)
        .Include(s => s.Site)
        .Include(s => s.ProtocolVersion)
        .Include(s => s.StudyArm);

    /// <summary>Merged scalar/FK values a create/update/patch will end up with, used to validate
    /// as one unit regardless of which action (full or partial) produced them.</summary>
    private sealed record SubjectWriteValues(int PatientId, int StudyId, int? StudyArmId, int? SiteId, int? ProtocolVersionId, string Status);

    /// <summary>Validates FK targets and business-rule constraints. Uses tracking queries (not
    /// AsNoTracking) deliberately: loading the target entities into the change tracker here is
    /// what lets EF's automatic relationship-fixup resolve navigations on SaveChangesAsync,
    /// mirroring StudiesController.ValidateReferencesAsync.</summary>
    private async Task<string?> ValidateEditModelAsync(int? currentSubjectId, SubjectWriteValues values, CancellationToken cancellationToken)
    {
        if (await _db.Patients.FindAsync([values.PatientId], cancellationToken) == null)
            return "PatientId does not reference an existing patient.";
        if (await _db.Studies.FindAsync([values.StudyId], cancellationToken) == null)
            return "StudyId does not reference an existing study.";
        if (values.StudyArmId.HasValue)
        {
            var arm = await _db.StudyArms.FindAsync([values.StudyArmId.Value], cancellationToken);
            if (arm == null || arm.StudyId != values.StudyId)
                return "StudyArmId does not reference an arm belonging to the referenced study.";
        }
        if (values.SiteId.HasValue)
        {
            if (await _db.Sites.FindAsync([values.SiteId.Value], cancellationToken) == null)
                return "SiteId does not reference an existing site.";
        }
        if (values.ProtocolVersionId.HasValue)
        {
            var protocolVersion = await _db.ProtocolVersions.FindAsync([values.ProtocolVersionId.Value], cancellationToken);
            if (protocolVersion == null || protocolVersion.StudyId != values.StudyId)
                return "ProtocolVersionId does not reference a protocol version belonging to the referenced study.";
        }
        if (!SubjectStatusCatalog.AllStatuses.Contains(values.Status))
            return $"Status must be one of: {string.Join(", ", SubjectStatusCatalog.AllStatuses)}.";

        if (SubjectStatusCatalog.IsActiveCategory(values.Status))
        {
            var conflict = await _db.Subjects.AnyAsync(s =>
                s.PatientId == values.PatientId
                && s.StudyId == values.StudyId
                && s.Id != currentSubjectId
                && SubjectStatusCatalog.ActiveStatuses.Contains(s.Status), cancellationToken);
            if (conflict)
                return "This patient already has an Active-category subject for this study.";
        }
        return null;
    }

    /// <summary>List subjects with optional filtering and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubjectViewModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjects(
        [FromQuery] int? patientId,
        [FromQuery] int? studyId,
        [FromQuery] string? status,
        [FromQuery] int? skip,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var query = IncludeAll(_db.Subjects.AsQueryable());
        if (patientId.HasValue) query = query.Where(s => s.PatientId == patientId.Value);
        if (studyId.HasValue) query = query.Where(s => s.StudyId == studyId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);

        var effectiveSkip = Math.Max(0, skip ?? 0);
        var effectiveLimit = SubjectSearchLimits.ClampLimit(limit);
        var list = await query.OrderBy(s => s.Id).Skip(effectiveSkip).Take(effectiveLimit).ToListAsync(cancellationToken);
        return Ok(list.Select(SubjectMappingService.ToViewModel));
    }

    /// <summary>
    /// Get a list of Subjects. This endpoint implements a minimal OData-like interface.
    /// </summary>
    /// <param name="queryOptions">
    /// Optional OData query options header for compatibility with Clinical Conductor.
    /// Currently only simple paging via <paramref name="skip"/> and <paramref name="top"/> is honored.
    /// </param>
    /// <param name="studyId">Optional filter to only return subjects enrolled in this study.</param>
    /// <param name="skip">Number of items to skip (default 0).</param>
    /// <param name="top">Maximum number of items to return (default 100, capped per SubjectSearchLimits).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("odata")]
    [ProducesResponseType(typeof(ODataPageResult<SubjectViewModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjectsOData(
        [FromHeader(Name = "queryOptions")] string? queryOptions,
        [FromQuery] int? studyId,
        [FromQuery] int? skip,
        [FromQuery] int? top,
        CancellationToken cancellationToken)
    {
        var query = IncludeAll(_db.Subjects.AsQueryable());
        if (studyId.HasValue) query = query.Where(s => s.StudyId == studyId.Value);
        query = query.OrderBy(s => s.Id);

        var totalCount = await query.LongCountAsync(cancellationToken);

        var safeSkip = Math.Max(0, skip ?? 0);
        var safeTop = SubjectSearchLimits.ClampLimit(top);

        var entities = await query
            .Skip(safeSkip)
            .Take(safeTop)
            .ToListAsync(cancellationToken);

        return Ok(new ODataPageResult<SubjectViewModel>
        {
            Items = entities.Select(SubjectMappingService.ToViewModel).ToList(),
            Count = totalCount,
            NextPageLink = null
        });
    }

    /// <summary>Get a subject by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SubjectViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubject(int id, CancellationToken cancellationToken)
    {
        var subject = await IncludeAll(_db.Subjects.AsQueryable()).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (subject == null) return NotFound();
        return Ok(SubjectMappingService.ToViewModel(subject));
    }

    /// <summary>Creates a subject. Also creates its initial SubjectStatus row.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubjectViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubject([FromBody] SubjectCreateModel createModel, CancellationToken cancellationToken)
    {
        // status/studyArmId/protocolVersionId aren't part of SubjectCreateModel (research.md
        // Decision 13) — validated here as null/InitialStatus so ValidateEditModelAsync's
        // per-field checks apply only to what creation can actually set.
        var validationError = await ValidateEditModelAsync(
            null,
            new SubjectWriteValues(createModel.PatientId, createModel.StudyId, null, createModel.SiteId, null, SubjectStatusCatalog.InitialStatus),
            cancellationToken);
        if (validationError != null) return BadRequest(validationError);

        var subject = new Subject { Uid = Guid.NewGuid(), CreatedOn = DateTime.UtcNow };
        SubjectMappingService.ApplyCreateModel(subject, createModel);
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync(cancellationToken);

        _db.SubjectStatuses.Add(new SubjectStatus
        {
            SubjectId = subject.Id,
            StatusName = subject.Status,
            ChangedOn = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSubject), new { id = subject.Id, version = "1.0" }, SubjectMappingService.ToViewModel(subject));
    }

    /// <summary>Full update. A status change appends a new SubjectStatus row.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SubjectViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] SubjectEditModel editModel, CancellationToken cancellationToken)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (subject == null) return NotFound();

        var validationError = await ValidateEditModelAsync(
            id,
            new SubjectWriteValues(editModel.PatientId, editModel.StudyId, editModel.StudyArmId, editModel.SiteId, editModel.ProtocolVersionId, editModel.Status),
            cancellationToken);
        if (validationError != null) return BadRequest(validationError);

        var statusChanged = subject.Status != editModel.Status;
        SubjectMappingService.ApplyEditModel(subject, editModel);
        await AppendStatusHistoryIfChangedAsync(subject, statusChanged, cancellationToken);

        return Ok(SubjectMappingService.ToViewModel(subject));
    }

    /// <summary>Partial update. A status change appends a new SubjectStatus row.</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(SubjectViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatchSubject(int id, [FromBody] SubjectPatchModel patchModel, CancellationToken cancellationToken)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (subject == null) return NotFound();

        var mergedValues = new SubjectWriteValues(
            patchModel.PatientId ?? subject.PatientId,
            patchModel.StudyId ?? subject.StudyId,
            patchModel.StudyArmId.HasValue ? patchModel.StudyArmId : subject.StudyArmId,
            patchModel.SiteId.HasValue ? patchModel.SiteId : subject.SiteId,
            patchModel.ProtocolVersionId.HasValue ? patchModel.ProtocolVersionId : subject.ProtocolVersionId,
            patchModel.Status ?? subject.Status);

        var validationError = await ValidateEditModelAsync(id, mergedValues, cancellationToken);
        if (validationError != null) return BadRequest(validationError);

        var statusChanged = patchModel.Status != null && subject.Status != patchModel.Status;
        SubjectMappingService.ApplyPatchModel(subject, patchModel);
        await AppendStatusHistoryIfChangedAsync(subject, statusChanged, cancellationToken);

        return Ok(SubjectMappingService.ToViewModel(subject));
    }

    private async Task AppendStatusHistoryIfChangedAsync(Subject subject, bool statusChanged, CancellationToken cancellationToken)
    {
        if (statusChanged)
        {
            _db.SubjectStatuses.Add(new SubjectStatus
            {
                SubjectId = subject.Id,
                StatusName = subject.Status,
                ChangedOn = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Deletes a subject and its SubjectStatus history. StatusHistory is explicitly
    /// loaded (not left to database-level cascade) so EF's change tracker cascades the delete
    /// consistently across providers, including the InMemory provider used by tests.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubject(int id, CancellationToken cancellationToken)
    {
        var subject = await _db.Subjects.Include(s => s.StatusHistory).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (subject == null) return NotFound();
        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
