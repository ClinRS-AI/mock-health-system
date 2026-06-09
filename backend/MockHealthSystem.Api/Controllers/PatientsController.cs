using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockHealthSystem.Api.Models.Patients;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Controllers;

/// <summary>
/// Patient endpoints aligned with Clinical Conductor API (excluding touches).
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patients")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public sealed class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>Normalize to UTC so Npgsql accepts it for timestamp with time zone (rejects Unspecified).</summary>
    private static DateTime? ToUtc(DateTime? value)
    {
        if (value == null) return null;
        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            _ => value.Value.ToUniversalTime()
        };
    }

    public PatientsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Get a patient by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PatientViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatient(int id, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .Include(p => p.PrimarySite)
            .Include(p => p.Phones)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (patient == null) return NotFound();
        return Ok(PatientMappingService.ToViewModel(patient));
    }

    /// <summary>Update a patient.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PatientViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePatient(int id, [FromBody] PatientEditModel editModel, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.Include(p => p.PrimarySite).Include(p => p.Phones).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (patient == null) return NotFound();
        PatientMappingService.ApplyEditModel(patient, editModel);
        var existingPhones = await _db.PatientPhones.Where(ph => ph.PatientId == id).ToListAsync(cancellationToken);
        _db.PatientPhones.RemoveRange(existingPhones);
        SyncPhonesFromEdit(id, editModel);
        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(patient).Collection(p => p.Phones).LoadAsync(cancellationToken);
        return Ok(PatientMappingService.ToViewModel(patient));
    }

    /// <summary>Patch a patient.</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(PatientViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchPatient(int id, [FromBody] PatientPatchModel patchModel, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.Include(p => p.PrimarySite).Include(p => p.Phones).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (patient == null) return NotFound();
        PatientMappingService.ApplyPatchModel(patient, patchModel);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(PatientMappingService.ToViewModel(patient));
    }

    /// <summary>Delete a patient.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeletePatient(int id, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FindAsync([id], cancellationToken);
        if (patient == null) return NotFound();
        _db.Patients.Remove(patient);
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Conflict(); }
        return NoContent();
    }

    /// <summary>Get list of patients (OData-style endpoint; simple list without query options).</summary>
    [HttpGet("odata")]
    [ProducesResponseType(typeof(IEnumerable<PatientViewModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientsOData(CancellationToken cancellationToken)
    {
        var list = await _db.Patients.Include(p => p.PrimarySite).Include(p => p.Phones).OrderBy(p => p.Id).Take(100).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    /// <summary>Create a patient.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PatientViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePatient([FromBody] PatientEditModel editModel, CancellationToken cancellationToken)
    {
        var patient = new Patient { FirstName = editModel.FirstName, LastName = editModel.LastName };
        PatientMappingService.ApplyEditModel(patient, editModel);
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(cancellationToken);
        SyncPhonesFromEdit(patient.Id, editModel);
        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(patient).Reference(p => p.PrimarySite).LoadAsync(cancellationToken);
        await _db.Entry(patient).Collection(p => p.Phones).LoadAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPatient), new { id = patient.Id, version = "1.0" }, PatientMappingService.ToViewModel(patient));
    }

    /// <summary>Search patients by criteria.</summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(IEnumerable<PatientViewModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPatients([FromBody] PatientSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _db.Patients.Include(p => p.PrimarySite).Include(p => p.Phones).AsQueryable();
        if (!string.IsNullOrWhiteSpace(criteria.FirstName)) query = query.Where(p => p.FirstName != null && p.FirstName.StartsWith(criteria.FirstName));
        if (!string.IsNullOrWhiteSpace(criteria.LastName)) query = query.Where(p => p.LastName != null && p.LastName.StartsWith(criteria.LastName));
        if (!string.IsNullOrWhiteSpace(criteria.MiddleName)) query = query.Where(p => p.MiddleName != null && p.MiddleName.StartsWith(criteria.MiddleName));
        if (!string.IsNullOrWhiteSpace(criteria.Gender)) query = query.Where(p => p.GenderCode == criteria.Gender);
        if (!string.IsNullOrWhiteSpace(criteria.Email)) query = query.Where(p => (p.PrimaryEmailAddress != null && p.PrimaryEmailAddress.StartsWith(criteria.Email)) || (p.SecondaryEmailAddress != null && p.SecondaryEmailAddress.StartsWith(criteria.Email)));
        if (criteria.DateOfBirth.HasValue) query = query.Where(p => p.DateOfBirth.HasValue && p.DateOfBirth.Value.Date == criteria.DateOfBirth.Value.Date);
        if (!string.IsNullOrWhiteSpace(criteria.Zip)) query = query.Where(p => p.Zip != null && p.Zip.StartsWith(criteria.Zip));
        if (!string.IsNullOrWhiteSpace(criteria.City)) query = query.Where(p => p.City != null && p.City.StartsWith(criteria.City));
        if (!string.IsNullOrWhiteSpace(criteria.Status)) query = query.Where(p => p.Status == criteria.Status);
        var skip = Math.Max(0, criteria.Skip ?? 0);
        var limit = PatientSearchLimits.ClampLimit(criteria.Limit);
        var list = await query.OrderBy(p => p.Id).Skip(skip).Take(limit).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    /// <summary>Update patient status.</summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(PatientViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(int id, [FromBody] PatientStatusEditModel editModel, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.Include(p => p.PrimarySite).Include(p => p.Phones).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (patient == null) return NotFound();
        PatientMappingService.ApplyStatus(patient, editModel);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(PatientMappingService.ToViewModel(patient));
    }

    // ---- devices ----
    [HttpGet("{id:int}/devices")]
    [ProducesResponseType(typeof(IEnumerable<PatientMedicalDeviceViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientDevices(int id, CancellationToken cancellationToken)
    {
        if (await _db.Patients.FindAsync([id], cancellationToken) == null) return NotFound();
        var list = await _db.PatientMedicalDevices.Include(x => x.Device).Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    [HttpPost("{id:int}/devices")]
    [ProducesResponseType(typeof(IEnumerable<PatientMedicalDeviceViewModel>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPatientDevices(int id, [FromBody] IList<PatientMedicalDeviceEditModel> patientDevices, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FindAsync([id], cancellationToken);
        if (patient == null) return NotFound();
        var entities = patientDevices.Select(d => new PatientMedicalDevice { PatientId = id, DeviceId = d.DeviceId, Comment = d.Comment }).ToList();
        _db.PatientMedicalDevices.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var e in entities) await _db.Entry(e).Reference(x => x.Device).LoadAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPatientDevices), new { id, version = "1.0" }, entities.Select(PatientMappingService.ToViewModel));
    }

    // ---- allergies ----
    [HttpGet("{id:int}/allergies")]
    [ProducesResponseType(typeof(IEnumerable<PatientAllergiesViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientAllergies(int id, CancellationToken cancellationToken)
    {
        if (await _db.Patients.FindAsync([id], cancellationToken) == null) return NotFound();
        var list = await _db.PatientAllergies.Include(x => x.Allergy).Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToAllergyViewModel));
    }

    [HttpPost("{id:int}/allergies")]
    [ProducesResponseType(typeof(IEnumerable<PatientAllergiesViewModel>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPatientAllergies(int id, [FromBody] IList<PatientAllergyModel> patientAllergies, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FindAsync([id], cancellationToken);
        if (patient == null) return NotFound();
        var entities = patientAllergies.Select(a => new PatientAllergy { PatientId = id, AllergyId = a.AllergyId, Reaction = a.Reaction, Comment = a.Comment, StartDate = ToUtc(a.StartDate), EndDate = ToUtc(a.EndDate) }).ToList();
        _db.PatientAllergies.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var e in entities) await _db.Entry(e).Reference(x => x.Allergy).LoadAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPatientAllergies), new { id, version = "1.0" }, entities.Select(PatientMappingService.ToAllergyViewModel));
    }

    // ---- providers ----
    [HttpGet("{id:int}/providers")]
    [ProducesResponseType(typeof(IEnumerable<PatientProviderViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientProviders(int id, CancellationToken cancellationToken)
    {
        if (await _db.Patients.FindAsync([id], cancellationToken) == null) return NotFound();
        var list = await _db.PatientProviders.Include(x => x.Provider).ThenInclude(p => p!.ProviderType).Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    [HttpPost("{id:int}/providers")]
    [ProducesResponseType(typeof(IEnumerable<PatientProviderViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPatientProviders(int id, [FromBody] IList<PatientProviderEditModel> providers, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FindAsync([id], cancellationToken);
        if (patient == null) return NotFound();
        var existing = await _db.PatientProviders.Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        _db.PatientProviders.RemoveRange(existing);
        var entities = providers.Select(p => new PatientProvider { PatientId = id, ProviderId = p.ProviderId, Comment = p.Comment, StartDate = ToUtc(p.StartDate), EndDate = ToUtc(p.EndDate) }).ToList();
        _db.PatientProviders.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
        var list = await _db.PatientProviders.Include(x => x.Provider).ThenInclude(p => p!.ProviderType).Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    // ---- conditions ----
    [HttpGet("{id:int}/conditions")]
    [ProducesResponseType(typeof(IEnumerable<PatientConditionViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientConditions(int id, CancellationToken cancellationToken)
    {
        if (await _db.Patients.FindAsync([id], cancellationToken) == null) return NotFound();
        var list = await _db.PatientConditions.Include(x => x.Condition).Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    [HttpPost("{id:int}/conditions")]
    [ProducesResponseType(typeof(IEnumerable<PatientConditionViewModel>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPatientConditions(int id, [FromBody] IList<PatientConditionEditModel> patientConditions, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FindAsync([id], cancellationToken);
        if (patient == null) return NotFound();
        var entities = patientConditions.Select(c => new PatientCondition { PatientId = id, ConditionId = c.ConditionId, StartDate = ToUtc(c.StartDate), EndDate = ToUtc(c.EndDate), AgeAtOnset = c.AgeAtOnset?.ToString(), Comment = c.Comment }).ToList();
        _db.PatientConditions.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var e in entities) await _db.Entry(e).Reference(x => x.Condition).LoadAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPatientConditions), new { id, version = "1.0" }, entities.Select(PatientMappingService.ToViewModel));
    }

    // ---- procedures ----
    [HttpGet("{id:int}/procedures")]
    [ProducesResponseType(typeof(IEnumerable<PatientProcedureViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientProcedures(int id, CancellationToken cancellationToken)
    {
        if (await _db.Patients.FindAsync([id], cancellationToken) == null) return NotFound();
        var list = await _db.PatientProcedures.Include(x => x.Procedure).Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    [HttpPost("{id:int}/procedures")]
    [ProducesResponseType(typeof(IEnumerable<PatientProcedureViewModel>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPatientProcedures(int id, [FromBody] IList<PatientProcedureModel> patientProcedures, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FindAsync([id], cancellationToken);
        if (patient == null) return NotFound();
        var entities = patientProcedures.Select(pp => new PatientProcedure { PatientId = id, ProcedureId = pp.ProcedureId, Comment = pp.Comment, ProcedureBy = pp.ProcedureBy, Date = ToUtc(pp.Date) }).ToList();
        foreach (var e in entities) { if (e.ProcedureId.HasValue) { var pr = await _db.Procedures.FindAsync([e.ProcedureId.Value], cancellationToken); if (pr != null) e.Name = pr.Name; e.CptCode = pr?.CptCode; } }
        _db.PatientProcedures.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var e in entities) await _db.Entry(e).Reference(x => x.Procedure).LoadAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPatientProcedures), new { id, version = "1.0" }, entities.Select(PatientMappingService.ToViewModel));
    }

    // ---- medications ----
    [HttpGet("{id:int}/medications")]
    [ProducesResponseType(typeof(IEnumerable<PatientMedicationViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientMedications(int id, CancellationToken cancellationToken)
    {
        if (await _db.Patients.FindAsync([id], cancellationToken) == null) return NotFound();
        var list = await _db.PatientMedications.Include(x => x.Medication).Include(x => x.Route).Include(x => x.MedicationConditions).ThenInclude(mc => mc.PatientCondition).ThenInclude(pc => pc!.Condition).Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    [HttpPost("{id:int}/medications")]
    [ProducesResponseType(typeof(IEnumerable<PatientMedicationViewModel>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPatientMedications(int id, [FromBody] IList<PatientMedicationEditModel> patientMedications, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FindAsync([id], cancellationToken);
        if (patient == null) return NotFound();
        var entities = patientMedications.Select(pm => new PatientMedication { PatientId = id, MedicationId = pm.Id, RouteId = pm.RouteId, Dosage = pm.Dosage, StartDate = ToUtc(pm.StartDate), EndDate = ToUtc(pm.EndDate), Comment = pm.Comment }).ToList();
        _db.PatientMedications.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
        for (var i = 0; i < entities.Count; i++)
        {
            var conds = patientMedications[i].Conditions;
            if (conds != null) foreach (var c in conds) _db.PatientMedicationConditions.Add(new PatientMedicationCondition { PatientMedicationId = entities[i].Id, PatientConditionId = c.PatientConditionId });
        }
        await _db.SaveChangesAsync(cancellationToken);
        var list = await _db.PatientMedications.Include(x => x.Medication).Include(x => x.Route).Include(x => x.MedicationConditions).ThenInclude(mc => mc.PatientCondition).ThenInclude(pc => pc!.Condition).Where(x => x.PatientId == id && entities.Select(e => e.Id).Contains(x.Id)).ToListAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPatientMedications), new { id, version = "1.0" }, list.Select(PatientMappingService.ToViewModel));
    }

    // ---- immunizations ----
    [HttpGet("{id:int}/immunizations")]
    [ProducesResponseType(typeof(IEnumerable<PatientImmunizationViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientImmunizations(int id, CancellationToken cancellationToken)
    {
        if (await _db.Patients.FindAsync([id], cancellationToken) == null) return NotFound();
        var list = await _db.PatientImmunizations.Include(x => x.Immunization).Include(x => x.ImmunizationType).Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    [HttpPost("{id:int}/immunizations")]
    [ProducesResponseType(typeof(IEnumerable<PatientImmunizationViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPatientImmunizations(int id, [FromBody] IList<PatientImmunizationEditModel> patientImmunizations, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FindAsync([id], cancellationToken);
        if (patient == null) return NotFound();
        var entities = new List<PatientImmunization>();
        foreach (var pi in patientImmunizations)
        {
            var imm = await _db.Immunizations.FindAsync([pi.ImmunizationId], cancellationToken);
            entities.Add(new PatientImmunization { PatientId = id, ImmunizationId = pi.ImmunizationId, Name = imm?.Name, Comment = pi.Comment, Location = pi.Location, Date = ToUtc(pi.Date) });
        }
        _db.PatientImmunizations.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var e in entities) { await _db.Entry(e).Reference(x => x.Immunization).LoadAsync(cancellationToken); await _db.Entry(e).Reference(x => x.ImmunizationType).LoadAsync(cancellationToken); }
        return Ok(entities.Select(PatientMappingService.ToViewModel));
    }

    // ---- family-history ----
    [HttpGet("{id:int}/family-history")]
    [ProducesResponseType(typeof(IEnumerable<PatientFamilyHistoryViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientFamilyHistory(int id, CancellationToken cancellationToken)
    {
        if (await _db.Patients.FindAsync([id], cancellationToken) == null) return NotFound();
        var list = await _db.PatientFamilyHistories.Include(x => x.Condition).Include(x => x.Relation).Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    [HttpPost("{id:int}/family-history")]
    [ProducesResponseType(typeof(IEnumerable<PatientFamilyHistoryViewModel>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPatientFamilyHistory(int id, [FromBody] IList<PatientFamilyHistoryEditModel> patientFamilyHistory, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FindAsync([id], cancellationToken);
        if (patient == null) return NotFound();
        var entities = patientFamilyHistory.Select(f => new PatientFamilyHistory { PatientId = id, ConditionId = f.ConditionId, FamilyMemberId = f.FamilyMemberId, AgeAtOnset = f.AgeAtOnset?.ToString(), Comment = f.Comment, StartDate = ToUtc(f.StartDate), EndDate = ToUtc(f.EndDate) }).ToList();
        foreach (var e in entities) { var r = await _db.Relations.FindAsync([e.FamilyMemberId], cancellationToken); if (r != null) e.RelationName = r.Name; }
        _db.PatientFamilyHistories.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var e in entities) { await _db.Entry(e).Reference(x => x.Condition).LoadAsync(cancellationToken); await _db.Entry(e).Reference(x => x.Relation).LoadAsync(cancellationToken); }
        return CreatedAtAction(nameof(GetPatientFamilyHistory), new { id, version = "1.0" }, entities.Select(PatientMappingService.ToViewModel));
    }

    // ---- social-history ----
    [HttpGet("{id:int}/social-history")]
    [ProducesResponseType(typeof(IEnumerable<PatientLifeStylesHistoryViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientSocialHistory(int id, CancellationToken cancellationToken)
    {
        if (await _db.Patients.FindAsync([id], cancellationToken) == null) return NotFound();
        var list = await _db.PatientSocialHistoryEntries.Include(x => x.SocialHistory).ThenInclude(s => s!.Category).Where(x => x.PatientId == id).ToListAsync(cancellationToken);
        return Ok(list.Select(PatientMappingService.ToViewModel));
    }

    [HttpPost("{id:int}/social-history")]
    [ProducesResponseType(typeof(IEnumerable<PatientLifeStylesHistoryViewModel>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPatientSocialHistory(int id, [FromBody] IList<PatientLifeStylesHistoryEditModel> patientSocialHistory, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FindAsync([id], cancellationToken);
        if (patient == null) return NotFound();
        var entities = patientSocialHistory.Select(s => new PatientSocialHistoryEntry { PatientId = id, SocialHistoryId = s.SocialHistoryId, Comment = s.Comment }).ToList();
        _db.PatientSocialHistoryEntries.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var e in entities) await _db.Entry(e).Reference(x => x.SocialHistory).Query().Include(sh => sh!.Category).LoadAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPatientSocialHistory), new { id, version = "1.0" }, entities.Select(PatientMappingService.ToViewModel));
    }

    private void SyncPhonesFromEdit(int patientId, PatientEditModel editModel)
    {
        if (editModel.Phone1 != null && !string.IsNullOrWhiteSpace(editModel.Phone1.Number))
            _db.PatientPhones.Add(new PatientPhone { PatientId = patientId, Slot = 1, Number = editModel.Phone1.Number, RawNumber = new string(editModel.Phone1.Number.Where(char.IsDigit).ToArray()) });
        if (editModel.Phone2 != null && !string.IsNullOrWhiteSpace(editModel.Phone2.Number))
            _db.PatientPhones.Add(new PatientPhone { PatientId = patientId, Slot = 2, Number = editModel.Phone2.Number, RawNumber = new string(editModel.Phone2.Number.Where(char.IsDigit).ToArray()) });
        if (editModel.Phone3 != null && !string.IsNullOrWhiteSpace(editModel.Phone3.Number))
            _db.PatientPhones.Add(new PatientPhone { PatientId = patientId, Slot = 3, Number = editModel.Phone3.Number, RawNumber = new string(editModel.Phone3.Number.Where(char.IsDigit).ToArray()) });
        if (editModel.Phone4 != null && !string.IsNullOrWhiteSpace(editModel.Phone4.Number))
            _db.PatientPhones.Add(new PatientPhone { PatientId = patientId, Slot = 4, Number = editModel.Phone4.Number, RawNumber = new string(editModel.Phone4.Number.Where(char.IsDigit).ToArray()) });
    }
}
