using MockHealthSystem.Api.Models.Subjects;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class SubjectMappingServiceTests
{
    private static Patient MinimalPatient(int id = 5) => new()
    {
        Id = id,
        Uid = Guid.NewGuid(),
        FirstName = "Jane",
        MiddleName = "Q",
        LastName = "Doe",
        Title = "Ms.",
        GenderCode = "F",
        Race = "White",
        Ethnicity = "Not Hispanic or Latino",
        DateOfBirth = new DateTime(1985, 3, 15, 0, 0, 0, DateTimeKind.Utc),
        Status = "Active"
    };

    private static Study MinimalStudy(int id = 7) => new()
    {
        Id = id,
        Uid = Guid.NewGuid(),
        Name = "Acme Study",
        Status = "Enrolling"
    };

    [Fact]
    public void ToViewModel_MapsScalarFields_AndRequiredNestedPreviews()
    {
        var uid = Guid.NewGuid();
        var enrollmentDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var subject = new Subject
        {
            Id = 42,
            Uid = uid,
            PatientId = 5,
            StudyId = 7,
            Status = "Screened",
            ScreeningNumber = "SCR-0042",
            EnrollmentDate = enrollmentDate,
            CreatedOn = enrollmentDate,
            LastUpdatedOn = enrollmentDate,
            Patient = MinimalPatient(),
            Study = MinimalStudy()
        };

        var vm = SubjectMappingService.ToViewModel(subject);

        Assert.Equal(42, vm.Id);
        Assert.Equal(uid, vm.Uid);
        Assert.Equal(7, vm.Study.Id);
        Assert.Equal("Acme Study", vm.Study.Name);
        Assert.Equal(5, vm.Patient.Id);
        Assert.Equal("Jane", vm.Patient.FirstName);
        Assert.Equal("Doe, Jane", vm.Patient.Name);
        Assert.Equal("Screened", vm.Status);
        Assert.Equal("SCR-0042", vm.ScreeningNumber);
        Assert.Equal(enrollmentDate, vm.EnrollmentDate);
        Assert.Null(vm.Site);
        Assert.Null(vm.ProtocolVersion);
        Assert.Null(vm.Arm);
    }

    [Fact]
    public void ToViewModel_MapsOptionalNestedPreviews_WhenPresent()
    {
        var site = new Site { Id = 11, Uid = Guid.NewGuid(), Name = "Main Site" };
        var arm = new StudyArm { Id = 12, Uid = Guid.NewGuid(), StudyId = 7, Name = "Arm A" };
        var protocolVersion = new ProtocolVersion { Id = 13, Uid = Guid.NewGuid(), StudyId = 7, Name = "v1" };
        var subject = new Subject
        {
            Id = 1,
            PatientId = 5,
            StudyId = 7,
            Status = "Randomized",
            EnrollmentDate = DateTime.UtcNow,
            Patient = MinimalPatient(),
            Study = MinimalStudy(),
            Site = site,
            StudyArm = arm,
            ProtocolVersion = protocolVersion
        };

        var vm = SubjectMappingService.ToViewModel(subject);

        Assert.NotNull(vm.Site);
        Assert.Equal(11, vm.Site!.Id);
        Assert.NotNull(vm.Arm);
        Assert.Equal(12, vm.Arm!.Id);
        Assert.NotNull(vm.ProtocolVersion);
        Assert.Equal(13, vm.ProtocolVersion!.Id);
    }

    [Fact]
    public void ApplyEditModel_SetsAllFields_AndUpdatesLastUpdatedOn()
    {
        var subject = new Subject { CreatedOn = DateTime.UtcNow.AddDays(-1), LastUpdatedOn = DateTime.UtcNow.AddDays(-1) };
        var model = new SubjectEditModel
        {
            PatientId = 10,
            StudyId = 20,
            SiteId = 30,
            StudyArmId = 31,
            ProtocolVersionId = 32,
            Status = "Randomized",
            GenderCode = "F",
            Race = "Asian",
            Ethnicity = "Not Hispanic or Latino",
            ImportId = "IMP-1",
            Tag = "Cardiology",
            FacilityCode = "FAC01",
            EnrollmentDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EnrollmentLocation = "Springfield",
            ScreeningNumber = "SCR-1000",
            RandomizationNumber = "RND-1000",
            TreatmentStatus = "On Treatment",
            TreatmentStart = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc),
            Narrative = "Enrolled without incident."
        };

        SubjectMappingService.ApplyEditModel(subject, model);

        Assert.Equal(10, subject.PatientId);
        Assert.Equal(20, subject.StudyId);
        Assert.Equal(30, subject.SiteId);
        Assert.Equal(31, subject.StudyArmId);
        Assert.Equal(32, subject.ProtocolVersionId);
        Assert.Equal("Randomized", subject.Status);
        Assert.Equal("F", subject.GenderCode);
        Assert.Equal("Asian", subject.Race);
        Assert.Equal("IMP-1", subject.ImportId);
        Assert.Equal("Cardiology", subject.Tag);
        Assert.Equal("FAC01", subject.FacilityCode);
        Assert.Equal(model.EnrollmentDate, subject.EnrollmentDate);
        Assert.Equal("Springfield", subject.EnrollmentLocation);
        Assert.Equal("SCR-1000", subject.ScreeningNumber);
        Assert.Equal("RND-1000", subject.RandomizationNumber);
        Assert.Equal("On Treatment", subject.TreatmentStatus);
        Assert.Equal(model.TreatmentStart, subject.TreatmentStart);
        Assert.Equal("Enrolled without incident.", subject.Narrative);
        Assert.True(subject.LastUpdatedOn > subject.CreatedOn);
    }

    [Fact]
    public void ApplyCreateModel_SetsOnlyCreateFields_AndDefaultsStatus_AndLeavesArmAndProtocolVersionUnset()
    {
        var subject = new Subject { CreatedOn = DateTime.UtcNow.AddDays(-1), LastUpdatedOn = DateTime.UtcNow.AddDays(-1) };
        var model = new SubjectCreateModel
        {
            PatientId = 10,
            StudyId = 20,
            SiteId = 30,
            ImportId = "IMP-1",
            Tag = "Cardiology",
            FacilityCode = "FAC01",
            EnrollmentDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EnrollmentLocation = "Springfield",
            ScreeningNumber = "SCR-1000",
            RandomizationNumber = "RND-1000",
            TreatmentStatus = "On Treatment",
            TreatmentStart = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc),
            Narrative = "Enrolled without incident."
        };

        SubjectMappingService.ApplyCreateModel(subject, model);

        Assert.Equal(10, subject.PatientId);
        Assert.Equal(20, subject.StudyId);
        Assert.Equal(30, subject.SiteId);
        Assert.Equal(SubjectStatusCatalog.InitialStatus, subject.Status);
        Assert.Equal("IMP-1", subject.ImportId);
        Assert.Equal("Cardiology", subject.Tag);
        Assert.Equal("FAC01", subject.FacilityCode);
        Assert.Equal(model.EnrollmentDate, subject.EnrollmentDate);
        Assert.Equal("Springfield", subject.EnrollmentLocation);
        Assert.Equal("SCR-1000", subject.ScreeningNumber);
        Assert.Equal("RND-1000", subject.RandomizationNumber);
        Assert.Equal("On Treatment", subject.TreatmentStatus);
        Assert.Equal(model.TreatmentStart, subject.TreatmentStart);
        Assert.Equal("Enrolled without incident.", subject.Narrative);
        Assert.Null(subject.StudyArmId);
        Assert.Null(subject.ProtocolVersionId);
        Assert.Null(subject.GenderCode);
        Assert.Null(subject.Race);
        Assert.Null(subject.Ethnicity);
        Assert.True(subject.LastUpdatedOn > subject.CreatedOn);
    }

    [Fact]
    public void ApplyPatchModel_OnlyUpdatesProvidedFields()
    {
        var originalEnrollment = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var subject = new Subject
        {
            PatientId = 1,
            StudyId = 2,
            StudyArmId = 3,
            Status = "Prescreened",
            ScreeningNumber = "SCR-1",
            EnrollmentDate = originalEnrollment
        };
        var patch = new SubjectPatchModel { Status = "Screened" };

        SubjectMappingService.ApplyPatchModel(subject, patch);

        Assert.Equal("Screened", subject.Status);
        // Untouched fields remain as-is.
        Assert.Equal(1, subject.PatientId);
        Assert.Equal(2, subject.StudyId);
        Assert.Equal(3, subject.StudyArmId);
        Assert.Equal("SCR-1", subject.ScreeningNumber);
        Assert.Equal(originalEnrollment, subject.EnrollmentDate);
    }

    [Fact]
    public void ToViewModel_SubjectStatus_MapsScalarFields()
    {
        var changedOn = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var history = new SubjectStatus
        {
            Id = 5,
            SubjectId = 42,
            StatusName = "Randomized",
            ChangedOn = changedOn,
            Comment = "Advanced after screening"
        };

        var vm = SubjectMappingService.ToViewModel(history);

        Assert.Equal(5, vm.Id);
        Assert.Equal(42, vm.SubjectId);
        Assert.Equal("Randomized", vm.StatusName);
        Assert.Equal(changedOn, vm.ChangedOn);
        Assert.Equal("Advanced after screening", vm.Comment);
        Assert.Null(vm.ChangedBy);
    }
}
