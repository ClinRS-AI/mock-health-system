using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Studies;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Api.Services.AdminSession;
using MockHealthSystem.Api.Swagger;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// Study reference/lookup configuration (Categories, Subcategories, Types, Statuses, Groups).
/// This is Mock-Health-System admin configuration, not part of the CC-mirrored integration
/// surface — gated the same way as <c>test-data/patients/lookup</c>, not the active CC auth
/// mode. See research.md's "StudyLookupController uses admin authentication" decision.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system")]
[AllowAnonymous]
[RequiresAdminAuth]
public sealed class StudyLookupController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAdminRequestValidator _adminRequestValidator;

    public StudyLookupController(AppDbContext db, IAdminRequestValidator adminRequestValidator)
    {
        _db = db;
        _adminRequestValidator = adminRequestValidator;
    }

    [HttpGet("study-categories")]
    [ProducesResponseType(typeof(IEnumerable<StudyCategoryViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudyCategories(CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var list = await _db.StudyCategories.OrderBy(c => c.Name).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    [HttpGet("study-subcategories")]
    [ProducesResponseType(typeof(IEnumerable<StudySubcategoryViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudySubcategories(CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var list = await _db.StudySubcategories.OrderBy(s => s.Name).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    [HttpGet("study-types")]
    [ProducesResponseType(typeof(IEnumerable<StudyTypeViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudyTypes(CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var list = await _db.StudyTypes.OrderBy(t => t.Name).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    [HttpGet("study-statuses")]
    [ProducesResponseType(typeof(IEnumerable<StudyStatusTypeViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudyStatuses(CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var list = await _db.StudyStatusTypes.OrderBy(s => s.Name).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    [HttpGet("study-groups")]
    [ProducesResponseType(typeof(IEnumerable<StudyGroupViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudyGroups(CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var list = await _db.StudyGroups.OrderBy(g => g.Name).ToListAsync(cancellationToken);
        return Ok(list.Select(StudyMappingService.ToViewModel));
    }

    [HttpGet("study-categories/{id:int}")]
    [ProducesResponseType(typeof(StudyCategoryViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudyCategory(int id, CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var category = await _db.StudyCategories.FindAsync([id], cancellationToken);
        if (category == null) return NotFound();
        return Ok(StudyMappingService.ToViewModel(category));
    }

    [HttpGet("study-subcategories/{id:int}")]
    [ProducesResponseType(typeof(StudySubcategoryViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudySubcategory(int id, CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var subcategory = await _db.StudySubcategories.FindAsync([id], cancellationToken);
        if (subcategory == null) return NotFound();
        return Ok(StudyMappingService.ToViewModel(subcategory));
    }

    [HttpPost("study-categories")]
    [ProducesResponseType(typeof(StudyCategoryViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateStudyCategory([FromBody] StudyCategoryEditModel editModel, CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var category = new StudyCategory { Name = editModel.Name, Description = editModel.Description };
        _db.StudyCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetStudyCategory), new { id = category.Id, version = "1.0" }, StudyMappingService.ToViewModel(category));
    }

    [HttpPut("study-categories/{id:int}")]
    [ProducesResponseType(typeof(StudyCategoryViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStudyCategory(int id, [FromBody] StudyCategoryEditModel editModel, CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var category = await _db.StudyCategories.FindAsync([id], cancellationToken);
        if (category == null) return NotFound();
        category.Name = editModel.Name;
        category.Description = editModel.Description;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(StudyMappingService.ToViewModel(category));
    }

    [HttpDelete("study-categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteStudyCategory(int id, CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var category = await _db.StudyCategories.FindAsync([id], cancellationToken);
        if (category == null) return NotFound();
        _db.StudyCategories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("study-subcategories")]
    [ProducesResponseType(typeof(StudySubcategoryViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateStudySubcategory([FromBody] StudySubcategoryEditModel editModel, CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var subcategory = new StudySubcategory { StudyCategoryId = editModel.StudyCategoryId, Name = editModel.Name, Description = editModel.Description };
        _db.StudySubcategories.Add(subcategory);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetStudySubcategory), new { id = subcategory.Id, version = "1.0" }, StudyMappingService.ToViewModel(subcategory));
    }

    [HttpPut("study-subcategories/{id:int}")]
    [ProducesResponseType(typeof(StudySubcategoryViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStudySubcategory(int id, [FromBody] StudySubcategoryEditModel editModel, CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var subcategory = await _db.StudySubcategories.FindAsync([id], cancellationToken);
        if (subcategory == null) return NotFound();
        subcategory.StudyCategoryId = editModel.StudyCategoryId;
        subcategory.Name = editModel.Name;
        subcategory.Description = editModel.Description;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(StudyMappingService.ToViewModel(subcategory));
    }

    [HttpDelete("study-subcategories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteStudySubcategory(int id, CancellationToken cancellationToken)
    {
        if (!_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)) return Forbid();
        var subcategory = await _db.StudySubcategories.FindAsync([id], cancellationToken);
        if (subcategory == null) return NotFound();
        _db.StudySubcategories.Remove(subcategory);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
