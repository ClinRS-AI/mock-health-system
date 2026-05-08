using System.Linq;
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
/// Endpoints for generating and resetting patient test data (including near-duplicates).
/// Protected by the AUTH_SETTINGS_ADMIN_KEY / X-Admin-Key mechanism.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/test-data")]
[AllowAnonymous]
public sealed class TestDataController : ControllerBase
{
    private readonly AppDbContext _db;

    public TestDataController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Generates synthetic patients and near-duplicate records for testing.
    /// </summary>
    /// <param name="request">Generation options. Defaults: 5000 patients, 3% duplicates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("patients/generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GeneratePatientsAsync(
        [FromBody] GeneratePatientsRequest? request,
        CancellationToken cancellationToken)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        var totalCount = request?.TotalCount ?? 5000;
        if (totalCount <= 0)
        {
            return BadRequest("TotalCount must be greater than zero.");
        }

        var duplicatePercentage = request?.DuplicatePercentage ?? 3;
        if (duplicatePercentage < 0 || duplicatePercentage > 100)
        {
            return BadRequest("DuplicatePercentage must be between 0 and 100.");
        }

        var seed = request?.Seed;

        var siteIds = await _db.Sites
            .AsNoTracking()
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var providerIds = await _db.Providers
            .AsNoTracking()
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var conditionIds = await _db.Conditions
            .AsNoTracking()
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var fakerService = new PatientFakerService(seed, siteIds);
        var patients = fakerService.CreatePatients(totalCount).ToList();

        // Persist base patients first so we can base duplicates on actual stored entities if needed.
        await _db.Patients.AddRangeAsync(patients, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        // Compute approximate duplicate count and create near-duplicates.
        var duplicateCount = (int)Math.Round(totalCount * (duplicatePercentage / 100.0), MidpointRounding.AwayFromZero);
        duplicateCount = Math.Max(0, duplicateCount);

        var duplicatePatients = new List<Patient>(capacity: duplicateCount);

        if (duplicateCount > 0)
        {
            var rng = seed.HasValue ? new Random(seed.Value + 1) : new Random();
            var basePatients = await _db.Patients
                .AsNoTracking()
                .OrderByDescending(p => p.Id)
                .Take(totalCount)
                .ToListAsync(cancellationToken);

            for (var i = 0; i < duplicateCount; i++)
            {
                var source = basePatients[rng.Next(basePatients.Count)];
                duplicatePatients.Add(CreateDuplicatePatientVariant(source, rng));
            }

            await _db.Patients.AddRangeAsync(duplicatePatients, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Optional: add 0-2 phones, 0-2 patient-providers, 0-2 patient-conditions per new patient.
        var newPatientIds = await _db.Patients
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .Take(totalCount + duplicateCount)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var relRng = seed.HasValue ? new Random(seed.Value + 2) : new Random();
        var phones = new List<PatientPhone>();
        var patientProviders = new List<PatientProvider>();
        var patientConditions = new List<PatientCondition>();

        foreach (var patientId in newPatientIds)
        {
            // 2-4 phones so Phone1-4 are often populated; some patients have 2 for null-handling tests
            var phoneCount = relRng.Next(2, 5);
            for (var s = 0; s < phoneCount; s++)
            {
                var num = relRng.Next(200, 999) * 10000000 + relRng.Next(0, 9999999);
                phones.Add(new PatientPhone
                {
                    PatientId = patientId,
                    Slot = s + 1,
                    Number = num.ToString("###-###-####"),
                    RawNumber = num.ToString("##########"),
                    OutOfService = relRng.Next(100) < 5
                });
            }

            if (providerIds.Count > 0)
            {
                var providerCount = relRng.Next(1, 3);
                var chosen = new HashSet<int>();
                for (var i = 0; i < providerCount && chosen.Count < providerIds.Count; i++)
                {
                    var providerId = providerIds[relRng.Next(providerIds.Count)];
                    if (chosen.Add(providerId))
                    {
                        patientProviders.Add(new PatientProvider
                        {
                            PatientId = patientId,
                            ProviderId = providerId,
                            StartDate = DateTime.UtcNow.AddDays(-relRng.Next(30, 365))
                        });
                    }
                }
            }

            if (conditionIds.Count > 0)
            {
                var conditionCount = relRng.Next(1, 3);
                var chosen = new HashSet<int>();
                for (var i = 0; i < conditionCount && chosen.Count < conditionIds.Count; i++)
                {
                    var conditionId = conditionIds[relRng.Next(conditionIds.Count)];
                    if (chosen.Add(conditionId))
                    {
                        patientConditions.Add(new PatientCondition
                        {
                            PatientId = patientId,
                            ConditionId = conditionId,
                            StartDate = DateTime.UtcNow.AddDays(-relRng.Next(60, 1825))
                        });
                    }
                }
            }
        }

        if (phones.Count > 0)
        {
            await _db.PatientPhones.AddRangeAsync(phones, cancellationToken);
        }
        if (patientProviders.Count > 0)
        {
            await _db.PatientProviders.AddRangeAsync(patientProviders, cancellationToken);
        }
        if (patientConditions.Count > 0)
        {
            await _db.PatientConditions.AddRangeAsync(patientConditions, cancellationToken);
        }
        if (phones.Count > 0 || patientProviders.Count > 0 || patientConditions.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            TotalRequested = totalCount,
            TotalBaseInserted = patients.Count,
            DuplicateRequested = duplicateCount,
            DuplicateInserted = duplicatePatients.Count,
            TotalAfter = await _db.Patients.CountAsync(cancellationToken)
        });
    }

    /// <summary>
    /// Resets all patient-related data using TRUNCATE. Use Generate patients to repopulate with a consistent dataset.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("patients/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetPatientsAsync(CancellationToken cancellationToken)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        const string truncateSql = """
TRUNCATE TABLE
    "PatientMedicationConditions",
    "PatientFamilyHistories",
    "PatientImmunizations",
    "PatientMedicalDevices",
    "PatientMedications",
    "PatientPhones",
    "PatientProcedures",
    "PatientProviders",
    "PatientSocialHistoryEntries",
    "PatientConditions",
    "PatientAllergies",
    "Patients"
RESTART IDENTITY CASCADE;
""";

        await _db.Database.ExecuteSqlRawAsync(truncateSql, cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Generates synthetic staff records for testing audit features.
    /// </summary>
    /// <param name="request">Generation options. Default count: 10.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("staff/generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GenerateStaffAsync(
        [FromBody] GenerateStaffRequest? request,
        CancellationToken cancellationToken)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        var count = request?.Count ?? 10;
        if (count <= 0)
        {
            return BadRequest("Count must be greater than zero.");
        }

        var seed = request?.Seed;
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        var firstNames = new[]
        {
            "Alex", "Jamie", "Taylor", "Jordan", "Morgan", "Casey", "Riley", "Avery", "Cameron", "Devin",
            "Sam", "Quinn", "Skyler", "Parker", "Drew", "Elliot", "Harper", "Rowan", "Charlie", "Blake"
        };
        var lastNames = new[]
        {
            "Morgan", "Taylor", "Parker", "Campbell", "Nguyen", "Patel", "Kim", "Lopez", "Reed", "Carter",
            "Diaz", "Brooks", "Ward", "Foster", "Bennett", "Hayes", "Coleman", "Murphy", "Ross", "Price"
        };

        var created = new List<Staff>(count);
        for (var i = 0; i < count; i++)
        {
            created.Add(new Staff
            {
                StaffUid = Guid.NewGuid(),
                FirstName = firstNames[rng.Next(firstNames.Length)],
                LastName = lastNames[rng.Next(lastNames.Length)],
                IsActive = true
            });
        }

        await _db.Staff.AddRangeAsync(created, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new GenerateStaffResponse
        {
            Requested = count,
            Inserted = created.Count,
            TotalAfter = await _db.Staff.CountAsync(cancellationToken)
        });
    }

    /// <summary>
    /// Generates recent audit events (within the last 5 minutes) for existing staff and patients.
    /// </summary>
    /// <param name="request">Generation options. Default count: 25.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("audit-events/generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GenerateRecentAuditEventsAsync(
        [FromBody] GenerateRecentAuditEventsRequest? request,
        CancellationToken cancellationToken)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        var count = request?.Count ?? 25;
        if (count <= 0)
        {
            return BadRequest("Count must be greater than zero.");
        }

        var staffRows = await _db.Staff
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);
        if (staffRows.Count == 0)
        {
            return BadRequest("No active staff found. Generate staff first.");
        }

        var auditTypes = await _db.AuditEntryTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        if (auditTypes.Count == 0)
        {
            return BadRequest("No audit entry types found.");
        }

        if (!auditTypes.Any(a => string.Equals(a.Code, "PATIENT_CREATED", StringComparison.OrdinalIgnoreCase))
            && await _db.Patients.CountAsync(cancellationToken) == 0)
        {
            return BadRequest(
                "No patients exist yet. Either seed patients or add a PATIENT_CREATED audit entry type so new patients can be created.");
        }

        var seed = request?.Seed;
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var now = DateTime.UtcNow;

        var inserted = 0;
        var insertedByType = new Dictionary<string, (int Count, string DisplayName)>(StringComparer.OrdinalIgnoreCase);

        void IncrementBreakdown(AuditEntryType auditType)
        {
            var key = auditType.Code.Trim();
            insertedByType.TryGetValue(key, out var cur);
            insertedByType[key] = (cur.Count + 1, auditType.DisplayName);
        }

        for (var i = 0; i < count; i++)
        {
            var patientCount = await _db.Patients.CountAsync(cancellationToken);
            var eligibleTypes = GetEligibleAuditTypesForGeneration(auditTypes, patientCount).ToList();
            if (eligibleTypes.Count == 0)
            {
                return BadRequest("No audit entry types can be applied given the current patient/staff data.");
            }

            var type = eligibleTypes[rng.Next(eligibleTypes.Count)];
            var code = type.Code.Trim();

            var activeStaff = await _db.Staff
                .Where(s => s.IsActive)
                .OrderBy(s => s.Id)
                .ToListAsync(cancellationToken);
            if (activeStaff.Count == 0)
            {
                return BadRequest("No active staff found. Generate staff first.");
            }

            var staffActor = activeStaff[rng.Next(activeStaff.Count)];
            var createdByUser = $"{staffActor.FirstName}.{staffActor.LastName}".ToLowerInvariant();
            var eventTime = now.AddSeconds(-rng.Next(0, 301));

            var log = new AuditLog
            {
                StaffPKey = staffActor.Id,
                StudyPKey = $"STUDY-{rng.Next(100, 999)}",
                CreatedTimeUtc = eventTime,
                CreatedByUser = createdByUser,
                AuditEntryTypeId = type.Id,
                Details = $"Synthetic {type.DisplayName} (test data)",
                SourceSystem = "MockHealthSystem"
            };

            int? auditPatientKey = null;

            if (string.Equals(code, "PATIENT_CREATED", StringComparison.OrdinalIgnoreCase))
            {
                auditPatientKey = await CreateSyntheticPatientAsync(rng, cancellationToken);
                log.Details = $"{log.Details} Created patient Id {auditPatientKey}.";
                log.PatientPKey = auditPatientKey;
            }
            else if (string.Equals(code, "PATIENT_DELETED", StringComparison.OrdinalIgnoreCase))
            {
                var victim = await PickRandomPatientEntityAsync(rng, cancellationToken);
                if (victim == null)
                {
                    i--;
                    continue;
                }

                log.PatientPKey = victim.Id;
                log.Details = $"{log.Details} Deleted patient Id {victim.Id}.";

                _db.AuditLogs.Add(log);
                await _db.SaveChangesAsync(cancellationToken);

                _db.Patients.Remove(victim);
                await _db.SaveChangesAsync(cancellationToken);
                IncrementBreakdown(type);
                inserted++;
                continue;
            }
            else if (string.Equals(code, "PATIENT_UPDATED", StringComparison.OrdinalIgnoreCase))
            {
                var patient = await PickRandomPatientEntityAsync(rng, cancellationToken);
                if (patient == null)
                {
                    i--;
                    continue;
                }

                MutatePatientForTest(patient, rng);
                auditPatientKey = patient.Id;
                log.PatientPKey = auditPatientKey;
                log.Details = $"{log.Details} Updated patient Id {patient.Id}.";
            }
            else if (string.Equals(code, "PATIENT_VIEWED", StringComparison.OrdinalIgnoreCase))
            {
                var patient = await PickRandomPatientEntityAsync(rng, cancellationToken);
                if (patient == null)
                {
                    i--;
                    continue;
                }

                TouchPatientViewed(patient);
                auditPatientKey = patient.Id;
                log.PatientPKey = auditPatientKey;
                log.Details = $"{log.Details} Touched patient Id {patient.Id} (view simulation).";
            }
            else if (string.Equals(code, "STAFF_PROFILE_UPDATED", StringComparison.OrdinalIgnoreCase))
            {
                MutateStaffProfile(staffActor, rng);
                log.Details = $"{log.Details} Updated staff Id {staffActor.Id}.";
            }
            else if (string.Equals(code, "LOGIN", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(code, "LOGOUT", StringComparison.OrdinalIgnoreCase))
            {
                // StaffUid is a stable secondary identifier, not a session token. Staff has no last-login
                // timestamp field, so LOGIN/LOGOUT are audit-only for now.
                log.Details =
                    $"{log.Details} No staff row mutation (audit-only; add LastLoginAtUtc to Staff to simulate).";
            }
            else
            {
                if (code.Contains("PATIENT", StringComparison.OrdinalIgnoreCase))
                {
                    var patient = await PickRandomPatientEntityAsync(rng, cancellationToken);
                    if (patient != null)
                    {
                        log.PatientPKey = patient.Id;
                    }
                }

                log.Details = $"{log.Details} (no specific test-data mutation implemented for this audit type).";
            }

            if (log.PatientPKey == null)
            {
                log.PatientPKey = auditPatientKey;
            }

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);
            IncrementBreakdown(type);
            inserted++;
        }

        var breakdown = insertedByType
            .Select(kv => new AuditTypeInsertCountDto
            {
                Code = kv.Key,
                DisplayName = kv.Value.DisplayName,
                Count = kv.Value.Count
            })
            .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Ok(new GenerateRecentAuditEventsResponse
        {
            Requested = count,
            Inserted = inserted,
            TotalAfter = await _db.AuditLogs.CountAsync(cancellationToken),
            InsertedByAuditType = breakdown
        });
    }

    /// <summary>
    /// Adds a single test patient with minimal fields (FirstName, LastName, Email). Id and Uid are auto-generated.
    /// </summary>
    /// <param name="request">FirstName, LastName, and Email (primary email address).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("patients/add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddTestPatientAsync(
        [FromBody] AddTestPatientRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        if (request == null || string.IsNullOrWhiteSpace(request.FirstName))
        {
            return BadRequest("FirstName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest("LastName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email is required.");
        }

        var patient = new Patient
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DisplayName = $"{request.LastName.Trim()}, {request.FirstName.Trim()}",
            Status = "Active",
            PrimaryEmailAddress = request.Email.Trim(),
            Uid = Guid.NewGuid()
        };

        await _db.Patients.AddAsync(patient, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AddTestPatientResponse
        {
            Id = patient.Id,
            Uid = patient.Uid!.Value
        });
    }

    /// <summary>
    /// Looks up a single patient by ID, UID, or email (first match wins). For use on the Test data management page.
    /// </summary>
    /// <param name="id">Patient ID (integer).</param>
    /// <param name="uid">Patient UID (GUID).</param>
    /// <param name="email">Primary or secondary email (match on start of value).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("patients/lookup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LookupPatientAsync(
        [FromQuery] int? id,
        [FromQuery] Guid? uid,
        [FromQuery] string? email,
        CancellationToken cancellationToken)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        if (!id.HasValue && !uid.HasValue && string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Provide one of: id, uid, or email.");
        }

        var query = _db.Patients
            .Include(p => p.PrimarySite)
            .Include(p => p.Phones)
            .AsQueryable();

        Patient? patient = null;
        if (id.HasValue)
        {
            patient = await query.FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken);
        }
        else if (uid.HasValue)
        {
            patient = await query.FirstOrDefaultAsync(p => p.Uid == uid.Value, cancellationToken);
        }
        else
        {
            var emailTrim = email!.Trim();
            patient = await query.FirstOrDefaultAsync(
                p => (p.PrimaryEmailAddress != null && p.PrimaryEmailAddress.StartsWith(emailTrim, StringComparison.OrdinalIgnoreCase))
                    || (p.SecondaryEmailAddress != null && p.SecondaryEmailAddress.StartsWith(emailTrim, StringComparison.OrdinalIgnoreCase)),
                cancellationToken);
        }

        if (patient == null)
        {
            return NotFound();
        }

        return Ok(PatientMappingService.ToViewModel(patient));
    }

    /// <summary>
    /// Looks up a random patient record from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("patients/random")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRandomPatientAsync(CancellationToken cancellationToken)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        var patientCount = await _db.Patients.CountAsync(cancellationToken);
        if (patientCount == 0)
        {
            return NotFound();
        }

        var randomIndex = Random.Shared.Next(patientCount);
        var patient = await _db.Patients
            .Include(p => p.PrimarySite)
            .Include(p => p.Phones)
            .OrderBy(p => p.Id)
            .Skip(randomIndex)
            .FirstOrDefaultAsync(cancellationToken);

        return patient == null ? NotFound() : Ok(PatientMappingService.ToViewModel(patient));
    }

    /// <summary>
    /// Updates a patient record by ID for test-data management workflows.
    /// </summary>
    /// <param name="id">Patient ID.</param>
    /// <param name="request">Updated patient record values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="saveWithAudit">When true, also creates a simulated PATIENT_UPDATED audit log for a random active staff member.</param>
    [HttpPut("patients/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePatientAsync(
        [FromRoute] int id,
        [FromBody] UpdateTestPatientRequest? request,
        CancellationToken cancellationToken,
        [FromQuery] bool saveWithAudit)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        if (request == null)
        {
            return BadRequest("Request body is required.");
        }

        var patient = await _db.Patients
            .Include(p => p.PrimarySite)
            .Include(p => p.Phones)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (patient == null)
        {
            return NotFound();
        }

        patient.PrimarySiteId = request.PrimarySite?.Id;
        patient.DisplayName = request.DisplayName;
        patient.Status = string.IsNullOrWhiteSpace(request.Status) ? patient.Status : request.Status.Trim();
        patient.StatusReason = request.StatusReason;
        patient.FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? patient.FirstName : request.FirstName.Trim();
        patient.MiddleName = request.MiddleName;
        patient.LastName = string.IsNullOrWhiteSpace(request.LastName) ? patient.LastName : request.LastName.Trim();
        patient.PhoneticName = request.PhoneticName;
        patient.PreferredName = request.PreferredName;
        patient.Title = request.Title;
        patient.PrimaryEmailAddress = request.PrimaryEmail?.Email;
        patient.PrimaryDoNotEmail = request.PrimaryEmail?.DoNotEmail ?? false;
        patient.SecondaryEmailAddress = request.SecondaryEmail?.Email;
        patient.SecondaryDoNotEmail = request.SecondaryEmail?.DoNotEmail ?? false;
        patient.Country = request.Country;
        patient.Address1 = request.Address1;
        patient.Address2 = request.Address2;
        patient.Address3 = request.Address3;
        patient.City = request.City;
        patient.State = request.State;
        patient.Zip = request.Zip;
        patient.DoNotMail = request.DoNotMail;
        patient.RecruitmentTextOptIn = request.RecruitmentTextOptIn;
        patient.PhoneTypeToText = request.PhoneTypeToText;
        patient.Fax = request.Fax;
        patient.DateOfBirth = request.DateOfBirth;
        patient.DateOfDeath = request.DateOfDeath;
        patient.GenderCode = request.GenderCode;
        patient.Race = request.Race;
        patient.Ethnicity = request.Ethnicity;
        patient.NativeLanguage = request.NativeLanguage;
        patient.MaritalStatus = request.MaritalStatus;
        patient.WeightValue = request.Weight?.Value;
        patient.WeightUnit = request.Weight?.Unit;
        patient.HeightValue = request.Height?.Value;
        patient.HeightUnit = request.Height?.Unit;
        patient.Ssn = request.Ssn;
        patient.Mrn = request.Mrn;
        patient.ImportId = request.ImportId;
        patient.ImportSourceId = request.ImportSourceId;
        patient.ImportPatientId = request.ImportPatientId;
        patient.Uid = request.Uid;
        patient.ManagedMedicare = request.ManagedMedicare;
        patient.CaregiverId = request.CaregiverId;
        patient.Caregiver = request.Caregiver;

        UpsertPatientPhone(patient, 1, request.Phone1);
        UpsertPatientPhone(patient, 2, request.Phone2);
        UpsertPatientPhone(patient, 3, request.Phone3);
        UpsertPatientPhone(patient, 4, request.Phone4);

        if (saveWithAudit)
        {
            var auditStaff = await _db.Staff
                .AsNoTracking()
                .Where(s => s.IsActive)
                .ToListAsync(cancellationToken);
            if (auditStaff.Count == 0)
            {
                return BadRequest("Save with Audit requires at least one active staff record.");
            }

            var auditType = await _db.AuditEntryTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Code == "PATIENT_UPDATED", cancellationToken);
            if (auditType == null)
            {
                return BadRequest("Save with Audit requires a PATIENT_UPDATED audit entry type.");
            }

            var staff = auditStaff[Random.Shared.Next(auditStaff.Count)];
            _db.AuditLogs.Add(new AuditLog
            {
                StaffPKey = staff.Id,
                PatientPKey = patient.Id,
                StudyPKey = $"STUDY-{Random.Shared.Next(100, 999)}",
                CreatedTimeUtc = DateTime.UtcNow,
                CreatedByUser = $"{staff.FirstName}.{staff.LastName}".ToLowerInvariant(),
                AuditEntryTypeId = auditType.Id,
                Details = $"Patient {patient.Id} updated via Test data management Save with Audit.",
                SourceSystem = "MockHealthSystem"
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var updated = await _db.Patients
            .Include(p => p.PrimarySite)
            .Include(p => p.Phones)
            .FirstAsync(p => p.Id == patient.Id, cancellationToken);

        return Ok(PatientMappingService.ToViewModel(updated));
    }

    /// <summary>
    /// Returns summary statistics for patient test data (counts and per-site distribution).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("patients/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientStatsAsync(CancellationToken cancellationToken)
    {
        if (!IsAdminRequest())
        {
            return Forbid();
        }

        var totalCount = await _db.Patients.CountAsync(cancellationToken);

        // Count duplicates based on marker in StatusReason (set for generated / baseline duplicates).
        var duplicateCount = await _db.Patients
            .CountAsync(
                p => p.StatusReason == "GeneratedDuplicate" || p.StatusReason == "BaselineDuplicate",
                cancellationToken);
        var fiveMinutesAgoUtc = DateTime.UtcNow.AddMinutes(-5);
        var recentAuditEventCount = await _db.AuditLogs
            .AsNoTracking()
            .CountAsync(a => a.CreatedTimeUtc >= fiveMinutesAgoUtc, cancellationToken);
        var totalStaffCount = await _db.Staff
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // Group by site name (or "Unassigned" when PrimarySiteId is null).
        var bySite = await _db.Patients
            .AsNoTracking()
            .GroupJoin(
                _db.Sites.AsNoTracking(),
                p => p.PrimarySiteId,
                s => s.Id,
                (p, sites) => new { Patient = p, Site = sites.FirstOrDefault() })
            .GroupBy(
                x => x.Site != null && !string.IsNullOrWhiteSpace(x.Site.Name) ? x.Site.Name : "Unassigned")
            .Select(g => new PatientsBySiteDto
            {
                SiteName = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.SiteName)
            .ToListAsync(cancellationToken);

        var stats = new PatientTestDataStatsDto
        {
            PatientCount = totalCount,
            DuplicatePatientCount = duplicateCount,
            RecentAuditEventCount = recentAuditEventCount,
            TotalStaffCount = totalStaffCount,
            PatientsBySite = bySite
        };

        return Ok(stats);
    }

    private static Patient CreateDuplicatePatientVariant(Patient source, Random rng)
    {
        // Copy all faker-backed fields from source so duplicate inherits full demographics.
        var duplicate = new Patient
        {
            PrimarySiteId = source.PrimarySiteId,
            Status = source.Status,
            DateOfBirth = source.DateOfBirth,
            DateOfDeath = source.DateOfDeath,
            Country = source.Country,
            City = source.City,
            State = source.State,
            Zip = source.Zip,
            PrimaryDoNotEmail = source.PrimaryDoNotEmail,
            SecondaryDoNotEmail = source.SecondaryDoNotEmail,
            DoNotMail = source.DoNotMail,
            RecruitmentTextOptIn = source.RecruitmentTextOptIn,
            ManagedMedicare = source.ManagedMedicare,
            Caregiver = source.Caregiver,
            GenderCode = source.GenderCode,
            Race = source.Race,
            Ethnicity = source.Ethnicity,
            MaritalStatus = source.MaritalStatus,
            NativeLanguage = source.NativeLanguage,
            WeightValue = source.WeightValue,
            WeightUnit = source.WeightUnit,
            HeightValue = source.HeightValue,
            HeightUnit = source.HeightUnit,
            Ssn = source.Ssn,
            Mrn = source.Mrn,
            ImportSourceId = source.ImportSourceId,
            ImportPatientId = source.ImportPatientId,
            Uid = source.Uid,
            Title = source.Title,
            MiddleName = source.MiddleName,
            PreferredName = source.PreferredName,
            PhoneticName = source.PhoneticName,
            Address2 = source.Address2,
            Address3 = source.Address3,
            Fax = source.Fax,
            SecondaryEmailAddress = source.SecondaryEmailAddress,
            GuardianJson = source.GuardianJson,
            PrimaryInsuranceJson = source.PrimaryInsuranceJson,
            SecondaryInsuranceJson = source.SecondaryInsuranceJson,
            CustomFieldsJson = source.CustomFieldsJson,
            StatusReason = "GeneratedDuplicate"
        };

        // Mutate identity fields to create near-duplicate variation.
        duplicate.FirstName = MutateFirstName(source.FirstName, rng);
        duplicate.LastName = MutateLastName(source.LastName, rng);
        duplicate.DisplayName = $"{duplicate.LastName}, {duplicate.FirstName}";

        var baseAddress = source.Address1 ?? "100 Main St";
        duplicate.Address1 = MutateStreetAddress(baseAddress, rng);

        var baseEmail = source.PrimaryEmailAddress ?? "user@example.com";
        duplicate.PrimaryEmailAddress = MutateEmail(baseEmail, rng);

        return duplicate;
    }

    private static string MutateFirstName(string firstName, Random rng)
    {
        // Simple variations: nicknames or small spelling tweaks.
        return firstName switch
        {
            "John" => rng.Next(2) == 0 ? "Jon" : "Johnny",
            "Robert" => rng.Next(2) == 0 ? "Rob" : "Bob",
            "Katherine" => "Kathrine",
            "Michael" => rng.Next(2) == 0 ? "Mike" : "Micheal",
            "Sarah" => "Sara",
            _ => firstName.Length > 3 && rng.Next(2) == 0
                ? firstName[..^1] + "y"
                : firstName
        };
    }

    private static string MutateLastName(string lastName, Random rng)
    {
        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < 3)
        {
            return lastName;
        }

        // Occasionally transpose two adjacent characters near the middle.
        if (rng.Next(2) == 0)
        {
            var index = rng.Next(1, lastName.Length - 1);
            var chars = lastName.ToCharArray();
            (chars[index], chars[index + 1]) = (chars[index + 1], chars[index]);
            return new string(chars);
        }

        return lastName;
    }

    private static string MutateStreetAddress(string address1, Random rng)
    {
        // Bump the street number slightly or add an apartment.
        var parts = address1.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && int.TryParse(parts[0], out var number))
        {
            var newNumber = number + rng.Next(-5, 6);
            if (newNumber < 1) newNumber = 1;
            var suffix = rng.Next(2) == 0 ? string.Empty : $" Apt {rng.Next(1, 20)}";
            return $"{newNumber} {parts[1]}{suffix}";
        }

        return address1;
    }

    private static string MutateEmail(string email, Random rng)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex == email.Length - 1)
        {
            return email;
        }

        var local = email[..atIndex];
        var domain = email[(atIndex + 1)..];

        // Append a numeric suffix or slight variation in the local part.
        var suffix = rng.Next(2) == 0 ? rng.Next(1, 9999).ToString() : "_dup";
        return $"{local}{suffix}@{domain}";
    }

    private static IEnumerable<AuditEntryType> GetEligibleAuditTypesForGeneration(
        IReadOnlyList<AuditEntryType> all,
        int patientCount)
    {
        return all.Where(a => IsAuditTypeEligibleForSyntheticGeneration(a.Code, patientCount));
    }

    private static bool IsAuditTypeEligibleForSyntheticGeneration(string code, int patientCount)
    {
        var c = code.Trim();
        if (string.Equals(c, "PATIENT_CREATED", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(c, "PATIENT_DELETED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(c, "PATIENT_UPDATED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(c, "PATIENT_VIEWED", StringComparison.OrdinalIgnoreCase))
        {
            return patientCount > 0;
        }

        if (c.Contains("PATIENT", StringComparison.OrdinalIgnoreCase))
        {
            return patientCount > 0;
        }

        return true;
    }

    private async Task<int> CreateSyntheticPatientAsync(Random rng, CancellationToken cancellationToken)
    {
        var suffix = rng.Next(1_000_000);
        var patient = new Patient
        {
            FirstName = $"Synth{suffix}",
            LastName = $"Patient{suffix}",
            DisplayName = $"Patient{suffix}, Synth{suffix}",
            Status = "Active",
            PrimaryEmailAddress = $"synthetic.{suffix}@example.local",
            Uid = Guid.NewGuid()
        };

        await _db.Patients.AddAsync(patient, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return patient.Id;
    }

    private async Task<Patient?> PickRandomPatientEntityAsync(Random rng, CancellationToken cancellationToken)
    {
        var count = await _db.Patients.CountAsync(cancellationToken);
        if (count == 0)
        {
            return null;
        }

        var skip = rng.Next(count);
        return await _db.Patients
            .OrderBy(p => p.Id)
            .Skip(skip)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void MutatePatientForTest(Patient patient, Random rng)
    {
        switch (rng.Next(6))
        {
            case 0:
                patient.City = $"{patient.City ?? "City"}-{rng.Next(999)}";
                break;
            case 1:
                patient.Zip = rng.Next(10000, 99999).ToString("D5");
                break;
            case 2:
                patient.DoNotMail = !patient.DoNotMail;
                break;
            case 3:
                patient.RecruitmentTextOptIn = !patient.RecruitmentTextOptIn;
                break;
            case 4:
                patient.StatusReason = $"Mutated-{DateTime.UtcNow.Ticks % 100_000}";
                break;
            default:
                patient.PreferredName = $"Pref-{rng.Next(9999)}";
                break;
        }
    }

    private static void TouchPatientViewed(Patient patient)
    {
        patient.StatusReason = $"ViewSim-{DateTime.UtcNow:o}";
    }

    private static void MutateStaffProfile(Staff staff, Random rng)
    {
        var fn = staff.FirstName.Trim();
        if (fn.Length > 110)
        {
            fn = fn[..110];
        }

        var ln = staff.LastName.Trim();
        if (ln.Length > 110)
        {
            ln = ln[..110];
        }

        staff.FirstName = $"{fn}_{rng.Next(999)}";
        staff.LastName = $"{ln}_{rng.Next(999)}";
    }

    private static void UpsertPatientPhone(Patient patient, int slot, PatientPhoneViewModel? phoneModel)
    {
        var existing = patient.Phones.FirstOrDefault(p => p.Slot == slot);
        if (phoneModel == null)
        {
            if (existing != null)
            {
                patient.Phones.Remove(existing);
            }

            return;
        }

        if (existing == null)
        {
            patient.Phones.Add(new PatientPhone
            {
                Slot = slot,
                Number = phoneModel.Number,
                RawNumber = phoneModel.RawNumber,
                OutOfService = phoneModel.OutOfService
            });

            return;
        }

        existing.Number = phoneModel.Number;
        existing.RawNumber = phoneModel.RawNumber;
        existing.OutOfService = phoneModel.OutOfService;
    }

    private bool IsAdminRequest()
    {
        // Always allow in Development for convenience, even if an admin key is configured.
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var requiredKey = Environment.GetEnvironmentVariable("AUTH_SETTINGS_ADMIN_KEY");
        if (string.IsNullOrWhiteSpace(requiredKey))
        {
            // No key configured: treat all callers as admin (dev convenience).
            return true;
        }

        if (!Request.Headers.TryGetValue("X-Admin-Key", out var headerValues))
        {
            return false;
        }

        var provided = headerValues.ToString();
        return string.Equals(provided, requiredKey, StringComparison.Ordinal);
    }

    public sealed class GeneratePatientsRequest
    {
        public int? TotalCount { get; set; }
        public int? DuplicatePercentage { get; set; }
        public int? Seed { get; set; }
    }

    public sealed class AddTestPatientRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }

    public sealed class AddTestPatientResponse
    {
        public int Id { get; set; }
        public Guid Uid { get; set; }
    }

    public sealed class UpdateTestPatientRequest : PatientViewModel
    {
    }

    public sealed class GenerateStaffRequest
    {
        public int? Count { get; set; }
        public int? Seed { get; set; }
    }

    public sealed class GenerateStaffResponse
    {
        public int Requested { get; set; }
        public int Inserted { get; set; }
        public int TotalAfter { get; set; }
    }

    public sealed class GenerateRecentAuditEventsRequest
    {
        public int? Count { get; set; }
        public int? Seed { get; set; }
    }

    public sealed class AuditTypeInsertCountDto
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class GenerateRecentAuditEventsResponse
    {
        public int Requested { get; set; }
        public int Inserted { get; set; }
        public int TotalAfter { get; set; }
        public IReadOnlyList<AuditTypeInsertCountDto> InsertedByAuditType { get; set; } = Array.Empty<AuditTypeInsertCountDto>();
    }

    public sealed class PatientsBySiteDto
    {
        public string SiteName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class PatientTestDataStatsDto
    {
        public int PatientCount { get; set; }
        public int DuplicatePatientCount { get; set; }
        public int RecentAuditEventCount { get; set; }
        public int TotalStaffCount { get; set; }
        public IReadOnlyList<PatientsBySiteDto> PatientsBySite { get; set; } = Array.Empty<PatientsBySiteDto>();
    }
}

