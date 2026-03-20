using System.Linq;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public sealed class PatientsBySiteDto
    {
        public string SiteName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class PatientTestDataStatsDto
    {
        public int PatientCount { get; set; }
        public int DuplicatePatientCount { get; set; }
        public IReadOnlyList<PatientsBySiteDto> PatientsBySite { get; set; } = Array.Empty<PatientsBySiteDto>();
    }
}

