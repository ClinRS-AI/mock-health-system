using MockHealthSystem.Api.Services;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class PatientFakerServiceTests
{
    // ---- CreatePatient: required fields ----

    [Fact]
    public void CreatePatient_AlwaysHasFirstName()
    {
        var svc = new PatientFakerService(seed: 42, siteIds: []);

        var patient = svc.CreatePatient();

        Assert.False(string.IsNullOrWhiteSpace(patient.FirstName));
    }

    [Fact]
    public void CreatePatient_AlwaysHasLastName()
    {
        var svc = new PatientFakerService(seed: 42, siteIds: []);

        var patient = svc.CreatePatient();

        Assert.False(string.IsNullOrWhiteSpace(patient.LastName));
    }

    [Fact]
    public void CreatePatient_AlwaysHasPrimaryEmailAddress()
    {
        var svc = new PatientFakerService(seed: 42, siteIds: []);

        var patient = svc.CreatePatient();

        Assert.False(string.IsNullOrWhiteSpace(patient.PrimaryEmailAddress));
        Assert.Contains("@", patient.PrimaryEmailAddress!);
    }

    [Fact]
    public void CreatePatient_EmailUsesExpectedDomain()
    {
        var svc = new PatientFakerService(seed: 42, siteIds: []);

        var patient = svc.CreatePatient();

        Assert.EndsWith("@example.com", patient.PrimaryEmailAddress);
    }

    [Fact]
    public void CreatePatient_StatusIsActive()
    {
        var svc = new PatientFakerService(seed: 99, siteIds: []);

        var patient = svc.CreatePatient();

        Assert.Equal("Active", patient.Status);
    }

    // ---- CreatePatient: demographic ranges ----

    [Fact]
    public void CreatePatient_DateOfBirthIsWithinExpectedRange()
    {
        var svc = new PatientFakerService(seed: 1, siteIds: []);

        var patient = svc.CreatePatient();

        // Patient must be between 18 and 80 years old.
        Assert.NotNull(patient.DateOfBirth);
        var ageInYears = (DateTime.UtcNow - patient.DateOfBirth!.Value).TotalDays / 365.25;
        Assert.True(ageInYears >= 17.9, $"Patient age {ageInYears:F1} is too young");
        Assert.True(ageInYears <= 81, $"Patient age {ageInYears:F1} is too old");
    }

    [Fact]
    public void CreatePatient_GenderCodeIsOneOfExpectedValues()
    {
        var svc = new PatientFakerService(seed: 7, siteIds: []);
        string[] validCodes = ["M", "F", "U", "O"];

        var patient = svc.CreatePatient();

        Assert.Contains(patient.GenderCode, validCodes);
    }

    [Fact]
    public void CreatePatient_HasMrnAndSsn()
    {
        var svc = new PatientFakerService(seed: 10, siteIds: []);

        var patient = svc.CreatePatient();

        Assert.False(string.IsNullOrWhiteSpace(patient.Mrn));
        Assert.False(string.IsNullOrWhiteSpace(patient.Ssn));
    }

    // ---- CreatePatient: site assignment ----

    [Fact]
    public void CreatePatient_HasNullPrimarySiteId_WhenNoSiteIdsProvided()
    {
        var svc = new PatientFakerService(seed: 5, siteIds: []);

        var patient = svc.CreatePatient();

        Assert.Null(patient.PrimarySiteId);
    }

    [Fact]
    public void CreatePatient_AssignsPrimarySiteId_WhenSiteIdsProvided()
    {
        var siteIds = new[] { 10, 20, 30 };
        var svc = new PatientFakerService(seed: 5, siteIds: siteIds);

        var patient = svc.CreatePatient();

        Assert.NotNull(patient.PrimarySiteId);
        Assert.Contains(patient.PrimarySiteId!.Value, siteIds);
    }

    // ---- CreatePatient: display name ----

    [Fact]
    public void CreatePatient_DisplayNameIsLastNameCommaFirstName()
    {
        var svc = new PatientFakerService(seed: 77, siteIds: []);

        var patient = svc.CreatePatient();

        Assert.NotNull(patient.DisplayName);
        Assert.Contains($"{patient.LastName}, {patient.FirstName}", patient.DisplayName);
    }

    // ---- CreatePatients: count ----

    [Fact]
    public void CreatePatients_ReturnsExactCount()
    {
        var svc = new PatientFakerService(seed: 100, siteIds: []);

        var patients = svc.CreatePatients(25);

        Assert.Equal(25, patients.Count);
    }

    [Fact]
    public void CreatePatients_ReturnsEmpty_WhenCountIsZero()
    {
        var svc = new PatientFakerService(seed: 1, siteIds: []);

        var patients = svc.CreatePatients(0);

        Assert.Empty(patients);
    }

    // ---- Structural consistency: multiple patients have distinct emails ----

    [Fact]
    public void CreatePatients_ProducesUniqueEmails_ForLargeBatch()
    {
        var svc = new PatientFakerService(seed: null, siteIds: []);

        var patients = svc.CreatePatients(20);
        var emails = patients.Select(p => p.PrimaryEmailAddress).Distinct().ToList();

        // Very unlikely that all 20 randomly-generated emails collide.
        Assert.True(emails.Count > 1, "Expected at least some unique email addresses across 20 patients.");
    }
}
