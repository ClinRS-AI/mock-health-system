using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Studies;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// Study endpoints aligned with the Clinical Conductor API (core Study CRUD, personnel, type association).
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/studies")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class StudiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudiesController(AppDbContext db)
    {
        _db = db;
    }

    private IQueryable<Study> IncludeAll(IQueryable<Study> query) => query
        .Include(s => s.SponsorTeam)
        .Include(s => s.ManagingSite)
        .Include(s => s.LeadSourceStaff)
        .Include(s => s.TargetDates)
        .Include(s => s.Leadership).ThenInclude(l => l.Staff)
        .Include(s => s.CustomFieldValues)
        .Include(s => s.Contacts);

    /// <summary>Validates FK targets exist. Uses tracking queries (not AsNoTracking) deliberately:
    /// loading the target entities into the change tracker here is what lets EF's automatic
    /// relationship-fixup resolve ManagingSite/LeadSourceStaff/Leadership[].Staff navigations on
    /// SaveChangesAsync, so callers don't need to re-query the aggregate afterward.</summary>
    private async Task<string?> ValidateReferencesAsync(
        int? managingSiteId, int? leadSourceStaffId, IEnumerable<int?>? leadershipStaffIds,
        CancellationToken cancellationToken)
    {
        if (managingSiteId.HasValue && await _db.Sites.FindAsync([managingSiteId.Value], cancellationToken) == null)
            return "ManagingSiteId does not reference an existing site.";
        if (leadSourceStaffId.HasValue && await _db.Staff.FindAsync([leadSourceStaffId.Value], cancellationToken) == null)
            return "StudyLead.StaffId does not reference an existing staff member.";

        var distinctLeadershipStaffIds = leadershipStaffIds?.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (distinctLeadershipStaffIds is { Count: > 0 })
        {
            var trackedStaff = await _db.Staff.Where(s => distinctLeadershipStaffIds.Contains(s.Id)).ToListAsync(cancellationToken);
            if (trackedStaff.Count != distinctLeadershipStaffIds.Count)
                return "One or more leadership staffId values do not reference an existing staff member.";
        }
        return null;
    }

    /// <summary>Shared validation for CreateStudy/UpdateStudy, both of which take StudyEditModel.
    /// PatchStudy has its own inline sequence since it takes the differently-shaped StudyPatchModel.</summary>
    private async Task<string?> ValidateEditModelAsync(StudyEditModel editModel, CancellationToken cancellationToken)
    {
        if (await _db.SponsorTeams.FindAsync([editModel.SponsorTeamId], cancellationToken) == null)
            return "SponsorTeamId does not reference an existing sponsor team.";

        var referenceError = await ValidateReferencesAsync(
            editModel.ManagingSiteId, editModel.StudyLead?.StaffId, editModel.Leadership?.Select(l => l.StaffId), cancellationToken);
        if (referenceError != null) return referenceError;

        return StudyMappingService.ValidateContacts(editModel.Contacts);
    }

    /// <summary>List studies with optional filtering and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StudyViewModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudies(
        [FromQuery] string? name,
        [FromQuery] string? status,
        [FromQuery] string? category,
        [FromQuery] string? protocolNumber,
        [FromQuery] int? skip,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var query = IncludeAll(_db.Studies.AsQueryable());
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(s => s.Name.StartsWith(name));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(s => s.Category == category);
        if (!string.IsNullOrWhiteSpace(protocolNumber)) query = query.Where(s => s.ProtocolNumber != null && s.ProtocolNumber.StartsWith(protocolNumber));

        var effectiveSkip = Math.Max(0, skip ?? 0);
        var effectiveLimit = StudySearchLimits.ClampLimit(limit);
        var list = await query.OrderBy(s => s.Id).Skip(effectiveSkip).Take(effectiveLimit).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get list of studies (OData-style endpoint; simple list without query options).</summary>
    [HttpGet("odata")]
    [ProducesResponseType(typeof(IEnumerable<StudyViewModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudiesOData(CancellationToken cancellationToken)
    {
        var list = await IncludeAll(_db.Studies.AsQueryable()).OrderBy(s => s.Id).Take(100).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    /// <summary>Get a study by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudyViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudy(int id, CancellationToken cancellationToken)
    {
        var study = await IncludeAll(_db.Studies.AsQueryable()).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (study == null) return NotFound();
        return Ok(StudyMappingService.ToViewModel(study));
    }

    /// <summary>Aggregates a study's leadership and role-staff assignments into one read-only list.</summary>
    [HttpGet("{id:int}/personnel")]
    [ProducesResponseType(typeof(IEnumerable<StaffPreviewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudyPersonnel(int id, CancellationToken cancellationToken)
    {
        var study = await _db.Studies
            .Include(s => s.Leadership).ThenInclude(l => l.Staff)
            .Include(s => s.Roles).ThenInclude(r => r.RoleStaff).ThenInclude(rs => rs.Staff)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (study == null) return NotFound();

        var leadershipStaff = study.Leadership.Where(l => l.Staff != null).Select(l => l.Staff!);
        var roleStaff = study.Roles.SelectMany(r => r.RoleStaff).Where(rs => rs.Staff != null).Select(rs => rs.Staff!);
        var personnel = leadershipStaff.Concat(roleStaff)
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .Select(s => StudyMappingService.ToStaffPreview(s));
        return Ok(personnel);
    }

    /// <summary>Create a study.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StudyViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStudy([FromBody] StudyEditModel editModel, CancellationToken cancellationToken)
    {
        var editModelError = await ValidateEditModelAsync(editModel, cancellationToken);
        if (editModelError != null) return BadRequest(editModelError);

        var study = new Study { Uid = editModel.Uid ?? Guid.NewGuid(), CreatedOn = DateTime.UtcNow, LastUpdatedOn = DateTime.UtcNow };
        StudyMappingService.ApplyEditModel(study, editModel);
        StudyMappingService.SyncTargetDatesFromEdit(study, editModel);
        StudyMappingService.SyncLeadershipFromEdit(study, editModel);
        StudyMappingService.SyncCustomFieldsFromEdit(study, editModel);
        StudyMappingService.SyncContactsFromEdit(study, editModel);

        _db.Studies.Add(study);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await IncludeAll(_db.Studies.AsQueryable()).FirstAsync(s => s.Id == study.Id, cancellationToken);
        return CreatedAtAction(nameof(GetStudy), new { id = study.Id, version = "1.0" }, StudyMappingService.ToViewModel(created));
    }

    /// <summary>Update a study (full replace; embedded arrays are replaced wholesale).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StudyViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStudy(int id, [FromBody] StudyEditModel editModel, CancellationToken cancellationToken)
    {
        var study = await IncludeAll(_db.Studies.AsQueryable()).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (study == null) return NotFound();

        var editModelError = await ValidateEditModelAsync(editModel, cancellationToken);
        if (editModelError != null) return BadRequest(editModelError);

        StudyMappingService.ApplyEditModel(study, editModel);
        study.LastUpdatedOn = DateTime.UtcNow;

        study.TargetDates.Clear();
        study.Leadership.Clear();
        study.CustomFieldValues.Clear();
        study.Contacts.Clear();
        StudyMappingService.SyncTargetDatesFromEdit(study, editModel);
        StudyMappingService.SyncLeadershipFromEdit(study, editModel);
        StudyMappingService.SyncCustomFieldsFromEdit(study, editModel);
        StudyMappingService.SyncContactsFromEdit(study, editModel);

        // No re-query needed: ValidateEditModelAsync/ValidateReferencesAsync already loaded every
        // reassigned FK target (ManagingSite, LeadSourceStaff, Leadership[].Staff) into the change
        // tracker above, so EF's automatic relationship-fixup resolves those navigations as part of
        // SaveChangesAsync's DetectChanges pass.
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(StudyMappingService.ToViewModel(study));
    }

    /// <summary>Partially update a study; omitted fields (including omitted embedded array entries/slots) are left untouched.</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(StudyViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchStudy(int id, [FromBody] StudyPatchModel patchModel, CancellationToken cancellationToken)
    {
        var study = await IncludeAll(_db.Studies.AsQueryable()).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (study == null) return NotFound();

        if (patchModel.SponsorTeamId.HasValue && await _db.SponsorTeams.FindAsync([patchModel.SponsorTeamId.Value], cancellationToken) == null)
            return BadRequest("SponsorTeamId does not reference an existing sponsor team.");

        var referenceError = await ValidateReferencesAsync(
            patchModel.ManagingSiteId, patchModel.StudyLead?.StaffId, patchModel.Leadership?.Select(l => l.StaffId), cancellationToken);
        if (referenceError != null) return BadRequest(referenceError);

        StudyMappingService.ApplyPatchModel(study, patchModel);
        study.LastUpdatedOn = DateTime.UtcNow;

        // No re-query needed — see the comment in UpdateStudy: ValidateReferencesAsync already
        // loaded any reassigned FK target into the change tracker, so fixup handles navigations.
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(StudyMappingService.ToViewModel(study));
    }

    /// <summary>Delete a study. Cascades to all structural sub-resources.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStudy(int id, CancellationToken cancellationToken)
    {
        // Load every cascading collection so EF's change-tracker cascade-delete fixup can mark
        // them for deletion — the in-memory test provider (unlike relational providers with a
        // real ON DELETE CASCADE constraint) only cascades to navigation collections that are
        // actually loaded/tracked, not whatever the DB schema declares.
        var study = await _db.Studies
            .Include(s => s.TargetDates)
            .Include(s => s.Leadership)
            .Include(s => s.CustomFieldValues)
            .Include(s => s.Contacts)
            .Include(s => s.Arms)
            .Include(s => s.Visits).ThenInclude(v => v.VisitArms)
            .Include(s => s.Milestones)
            .Include(s => s.Documents).ThenInclude(d => d.StatusHistory)
            .Include(s => s.Notes)
            .Include(s => s.Roles).ThenInclude(r => r.RoleStaff)
            .Include(s => s.ProtocolVersions)
            .Include(s => s.StudyTypes)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (study == null) return NotFound();
        _db.Studies.Remove(study);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Associate a study type with a study.</summary>
    [HttpPost("{studyId:int}/types/add")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddStudyType(int studyId, [FromBody] AddStudyTypeRequest request, CancellationToken cancellationToken)
    {
        if (await _db.Studies.FindAsync([studyId], cancellationToken) == null) return NotFound();
        if (await _db.StudyTypes.FindAsync([request.StudyTypeId], cancellationToken) == null)
            return BadRequest("StudyTypeId does not reference an existing study type.");

        var alreadyLinked = await _db.StudyStudyTypes.AnyAsync(x => x.StudyId == studyId && x.StudyTypeId == request.StudyTypeId, cancellationToken);
        if (!alreadyLinked)
        {
            _db.StudyStudyTypes.Add(new StudyStudyType { StudyId = studyId, StudyTypeId = request.StudyTypeId });
            await _db.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    /// <summary>Remove a study type association from a study. <paramref name="id"/> is the StudyType id being removed.</summary>
    [HttpDelete("{studyId:int}/types/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveStudyType(int studyId, int id, CancellationToken cancellationToken)
    {
        var link = await _db.StudyStudyTypes.FirstOrDefaultAsync(x => x.StudyId == studyId && x.StudyTypeId == id, cancellationToken);
        if (link == null) return NotFound();
        _db.StudyStudyTypes.Remove(link);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    public sealed class AddStudyTypeRequest
    {
        public int StudyTypeId { get; set; }
    }
}
