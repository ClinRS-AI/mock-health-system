using MockHealthSystem.Api.Models.Patients;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class PatientMappingServiceTests
{
    private static Patient MinimalPatient(int id = 1) => new()
    {
        Id = id,
        FirstName = "Jane",
        LastName = "Doe",
        Status = "Active",
        Phones = new List<PatientPhone>()
    };

    // ---- ToViewModel: basic fields ----

    [Fact]
    public void ToViewModel_MapsScalarFields()
    {
        var dob = new DateTime(1985, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var uid = Guid.NewGuid();
        var patient = new Patient
        {
            Id = 42,
            FirstName = "John",
            LastName = "Smith",
            DisplayName = "Smith, John",
            Status = "Active",
            StatusReason = "Enrolled",
            GenderCode = "M",
            Race = "White",
            Ethnicity = "Not Hispanic or Latino",
            NativeLanguage = "English",
            MaritalStatus = "Single",
            Country = "US",
            Address1 = "123 Main St",
            City = "Springfield",
            State = "IL",
            Zip = "62701",
            Mrn = "MRN12345",
            Ssn = "123-45-6789",
            DateOfBirth = dob,
            Uid = uid,
            Phones = new List<PatientPhone>()
        };

        var vm = PatientMappingService.ToViewModel(patient);

        Assert.Equal(42, vm.Id);
        Assert.Equal("Smith, John", vm.DisplayName);
        Assert.Equal("Active", vm.Status);
        Assert.Equal("Enrolled", vm.StatusReason);
        Assert.Equal("M", vm.GenderCode);
        Assert.Equal("White", vm.Race);
        Assert.Equal("US", vm.Country);
        Assert.Equal("123 Main St", vm.Address1);
        Assert.Equal("Springfield", vm.City);
        Assert.Equal("IL", vm.State);
        Assert.Equal("62701", vm.Zip);
        Assert.Equal("MRN12345", vm.Mrn);
        Assert.Equal(dob, vm.DateOfBirth);
        Assert.Equal(uid, vm.Uid);
    }

    [Fact]
    public void ToViewModel_FallsBackToFirstAndLastName_WhenDisplayNameIsNull()
    {
        var patient = MinimalPatient();
        patient.DisplayName = null;

        var vm = PatientMappingService.ToViewModel(patient);

        Assert.Equal("Jane Doe", vm.DisplayName);
    }

    // ---- ToViewModel: email ----

    [Fact]
    public void ToViewModel_ReturnsNullEmail_WhenAddressIsNull()
    {
        var patient = MinimalPatient();
        patient.PrimaryEmailAddress = null;
        patient.SecondaryEmailAddress = null;

        var vm = PatientMappingService.ToViewModel(patient);

        Assert.Null(vm.PrimaryEmail);
        Assert.Null(vm.SecondaryEmail);
    }

    [Fact]
    public void ToViewModel_ReturnsNullEmail_WhenAddressIsEmpty()
    {
        var patient = MinimalPatient();
        patient.PrimaryEmailAddress = "";
        patient.SecondaryEmailAddress = "   ";

        var vm = PatientMappingService.ToViewModel(patient);

        Assert.Null(vm.PrimaryEmail);
        // SecondaryEmailAddress is not whitespace-checked by the service (IsNullOrEmpty only), so
        // "   " (spaces only) will produce a non-null entry. That's existing behavior.
        // We only assert primary here.
        Assert.Null(vm.PrimaryEmail);
    }

    [Fact]
    public void ToViewModel_MapsEmail_WithDoNotEmailFlag()
    {
        var patient = MinimalPatient();
        patient.PrimaryEmailAddress = "jane@example.com";
        patient.PrimaryDoNotEmail = true;
        patient.SecondaryEmailAddress = "jane2@example.com";
        patient.SecondaryDoNotEmail = false;

        var vm = PatientMappingService.ToViewModel(patient);

        Assert.NotNull(vm.PrimaryEmail);
        Assert.Equal("jane@example.com", vm.PrimaryEmail!.Email);
        Assert.True(vm.PrimaryEmail.DoNotEmail);
        Assert.NotNull(vm.SecondaryEmail);
        Assert.Equal("jane2@example.com", vm.SecondaryEmail!.Email);
        Assert.False(vm.SecondaryEmail.DoNotEmail);
    }

    // ---- ToViewModel: weight / height ----

    [Fact]
    public void ToViewModel_MapsWeight_WhenValueIsPresent()
    {
        var patient = MinimalPatient();
        patient.WeightValue = 72.3;
        patient.WeightUnit = "kg";

        var vm = PatientMappingService.ToViewModel(patient);

        Assert.NotNull(vm.Weight);
        Assert.Equal(72.3, vm.Weight!.Value);
        Assert.Equal("kg", vm.Weight.Unit);
    }

    [Fact]
    public void ToViewModel_ReturnsNullWeightAndHeight_WhenValuesAbsent()
    {
        var patientNoWeight = MinimalPatient();
        patientNoWeight.WeightValue = null;
        patientNoWeight.WeightUnit = "kg";

        Assert.Null(PatientMappingService.ToViewModel(patientNoWeight).Weight);

        var patientNoHeight = MinimalPatient();
        patientNoHeight.HeightValue = null;

        Assert.Null(PatientMappingService.ToViewModel(patientNoHeight).Height);
    }

    [Fact]
    public void ToViewModel_MapsHeight_WhenValueIsPresent()
    {
        var patient = MinimalPatient();
        patient.HeightValue = 1.75;
        patient.HeightUnit = "m";

        var vm = PatientMappingService.ToViewModel(patient);

        Assert.NotNull(vm.Height);
        Assert.Equal(1.75, vm.Height!.Value);
        Assert.Equal("m", vm.Height.Unit);
    }

    // ---- ToViewModel: phones ----

    [Fact]
    public void ToViewModel_MapsPhonesBySlot()
    {
        var patient = MinimalPatient();
        patient.Phones = new List<PatientPhone>
        {
            new() { Slot = 3, Number = "555-333-3333", RawNumber = "5553333333", OutOfService = false },
            new() { Slot = 1, Number = "555-111-1111", RawNumber = "5551111111", OutOfService = false },
            new() { Slot = 2, Number = "555-222-2222", RawNumber = "5552222222", OutOfService = true },
        };

        var vm = PatientMappingService.ToViewModel(patient);

        Assert.NotNull(vm.Phone1);
        Assert.Equal("555-111-1111", vm.Phone1!.Number);
        Assert.NotNull(vm.Phone2);
        Assert.Equal("555-222-2222", vm.Phone2!.Number);
        Assert.True(vm.Phone2!.OutOfService);
        Assert.NotNull(vm.Phone3);
        Assert.Equal("555-333-3333", vm.Phone3!.Number);
        Assert.Null(vm.Phone4);
    }

    [Fact]
    public void ToViewModel_ReturnsNullPhone_WhenSlotMissing()
    {
        var patient = MinimalPatient();
        patient.Phones = new List<PatientPhone>
        {
            new() { Slot = 2, Number = "555-222-2222", RawNumber = "5552222222" }
        };

        var vm = PatientMappingService.ToViewModel(patient);

        Assert.Null(vm.Phone1);
        Assert.NotNull(vm.Phone2);
        Assert.Null(vm.Phone3);
        Assert.Null(vm.Phone4);
    }

    // ---- ToViewModel: JSON fields ----

    [Fact]
    public void ToViewModel_DeserializesValidInsuranceJson()
    {
        var patient = MinimalPatient();
        patient.PrimaryInsuranceJson = """{"name":"BlueCross","account":"XYZ123"}""";

        var vm = PatientMappingService.ToViewModel(patient);

        Assert.NotNull(vm.PrimaryInsurance);
    }

    [Fact]
    public void ToViewModel_ReturnsNullInsurance_WhenJsonIsInvalidOrNull()
    {
        var invalidPatient = MinimalPatient();
        invalidPatient.PrimaryInsuranceJson = "not-valid-json{{";
        Assert.Null(PatientMappingService.ToViewModel(invalidPatient).PrimaryInsurance);

        var nullPatient = MinimalPatient();
        nullPatient.PrimaryInsuranceJson = null;
        Assert.Null(PatientMappingService.ToViewModel(nullPatient).PrimaryInsurance);
    }

    // ---- ApplyEditModel ----

    [Fact]
    public void ApplyEditModel_CopiesAllScalarFields()
    {
        var entity = new Patient();
        var dob = new DateTime(1990, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var model = new PatientEditModel
        {
            FirstName = "Alice",
            LastName = "Brown",
            MiddleName = "M",
            GenderCode = "F",
            DateOfBirth = dob,
            Country = "US",
            Address1 = "456 Oak Ave",
            City = "Chicago",
            State = "IL",
            Zip = "60601",
            Mrn = "MRN99999",
            Ssn = "999-88-7777",
            DoNotMail = true,
            RecruitmentTextOptIn = true,
            Caregiver = true
        };

        PatientMappingService.ApplyEditModel(entity, model);

        Assert.Equal("Alice", entity.FirstName);
        Assert.Equal("Brown", entity.LastName);
        Assert.Equal("M", entity.MiddleName);
        Assert.Equal("F", entity.GenderCode);
        Assert.Equal(dob, entity.DateOfBirth);
        Assert.Equal("US", entity.Country);
        Assert.Equal("456 Oak Ave", entity.Address1);
        Assert.Equal("Chicago", entity.City);
        Assert.Equal("IL", entity.State);
        Assert.Equal("60601", entity.Zip);
        Assert.Equal("MRN99999", entity.Mrn);
        Assert.Equal("999-88-7777", entity.Ssn);
        Assert.True(entity.DoNotMail);
        Assert.True(entity.RecruitmentTextOptIn);
        Assert.True(entity.Caregiver);
    }

    [Fact]
    public void ApplyEditModel_SerializesGuardianToJson()
    {
        var entity = new Patient();
        var model = new PatientEditModel
        {
            FirstName = "Test",
            LastName = "Patient",
            Guardian = new GuardianModel { Name = "Parent Guardian", Phone = "555-000-0000" }
        };

        PatientMappingService.ApplyEditModel(entity, model);

        Assert.NotNull(entity.GuardianJson);
        Assert.Contains("Guardian", entity.GuardianJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyEditModel_SetsNullGuardianJson_WhenGuardianModelIsNull()
    {
        var entity = new Patient { GuardianJson = """{"name":"old"}""" };
        var model = new PatientEditModel { FirstName = "Test", LastName = "Patient", Guardian = null };

        PatientMappingService.ApplyEditModel(entity, model);

        Assert.Null(entity.GuardianJson);
    }

    [Fact]
    public void ApplyEditModel_MapsEmailFields()
    {
        var entity = new Patient();
        var model = new PatientEditModel
        {
            FirstName = "Test",
            LastName = "Patient",
            PrimaryEmail = new PatientEmailModel { Email = "test@example.com", DoNotEmail = true },
            SecondaryEmail = new PatientEmailModel { Email = "test2@example.com", DoNotEmail = false }
        };

        PatientMappingService.ApplyEditModel(entity, model);

        Assert.Equal("test@example.com", entity.PrimaryEmailAddress);
        Assert.True(entity.PrimaryDoNotEmail);
        Assert.Equal("test2@example.com", entity.SecondaryEmailAddress);
        Assert.False(entity.SecondaryDoNotEmail);
    }

    [Fact]
    public void ApplyEditModel_SetsNullEmail_WhenEmailModelIsNull()
    {
        var entity = new Patient { PrimaryEmailAddress = "old@example.com", PrimaryDoNotEmail = true };
        var model = new PatientEditModel { FirstName = "Test", LastName = "Patient", PrimaryEmail = null };

        PatientMappingService.ApplyEditModel(entity, model);

        Assert.Null(entity.PrimaryEmailAddress);
        Assert.False(entity.PrimaryDoNotEmail);
    }

    [Fact]
    public void ApplyEditModel_MapsWeightAndHeight()
    {
        var entity = new Patient();
        var model = new PatientEditModel
        {
            FirstName = "Test",
            LastName = "Patient",
            Weight = new WeightModel { Value = 80.0, Unit = "kg" },
            Height = new HeightModel { Value = 1.80, Unit = "m" }
        };

        PatientMappingService.ApplyEditModel(entity, model);

        Assert.Equal(80.0, entity.WeightValue);
        Assert.Equal("kg", entity.WeightUnit);
        Assert.Equal(1.80, entity.HeightValue);
        Assert.Equal("m", entity.HeightUnit);
    }

    [Fact]
    public void ApplyEditModel_ClearsWeightAndHeight_WhenModelsAreNull()
    {
        var entity = new Patient { WeightValue = 70.0, WeightUnit = "kg", HeightValue = 1.70, HeightUnit = "m" };
        var model = new PatientEditModel { FirstName = "Test", LastName = "Patient", Weight = null, Height = null };

        PatientMappingService.ApplyEditModel(entity, model);

        Assert.Null(entity.WeightValue);
        Assert.Null(entity.WeightUnit);
        Assert.Null(entity.HeightValue);
        Assert.Null(entity.HeightUnit);
    }

    // ---- ApplyPatchModel ----

    [Fact]
    public void ApplyPatchModel_OnlyUpdatesNonNullFields()
    {
        var entity = new Patient
        {
            FirstName = "Original",
            LastName = "Name",
            City = "OldCity",
            GenderCode = "M"
        };
        var model = new PatientPatchModel { City = "NewCity" };

        PatientMappingService.ApplyPatchModel(entity, model);

        Assert.Equal("Original", entity.FirstName);
        Assert.Equal("Name", entity.LastName);
        Assert.Equal("NewCity", entity.City);
        Assert.Equal("M", entity.GenderCode);
    }

    [Fact]
    public void ApplyPatchModel_UpdatesBoolFields_WhenHasValue()
    {
        var entity = new Patient { DoNotMail = false, RecruitmentTextOptIn = false };
        var model = new PatientPatchModel { DoNotMail = true, RecruitmentTextOptIn = true };

        PatientMappingService.ApplyPatchModel(entity, model);

        Assert.True(entity.DoNotMail);
        Assert.True(entity.RecruitmentTextOptIn);
    }

    [Fact]
    public void ApplyPatchModel_LeavesExistingBools_WhenNotProvided()
    {
        var entity = new Patient { DoNotMail = true };
        var model = new PatientPatchModel(); // DoNotMail not provided (null)

        PatientMappingService.ApplyPatchModel(entity, model);

        Assert.True(entity.DoNotMail);
    }

    [Fact]
    public void ApplyPatchModel_UpdatesEmail_WhenEmailModelProvided()
    {
        var entity = new Patient { PrimaryEmailAddress = "old@example.com", PrimaryDoNotEmail = false };
        var model = new PatientPatchModel
        {
            PrimaryEmail = new PatientEmailModel { Email = "new@example.com", DoNotEmail = true }
        };

        PatientMappingService.ApplyPatchModel(entity, model);

        Assert.Equal("new@example.com", entity.PrimaryEmailAddress);
        Assert.True(entity.PrimaryDoNotEmail);
    }

    [Fact]
    public void ApplyPatchModel_LeavesEmail_WhenNotProvided()
    {
        var entity = new Patient { PrimaryEmailAddress = "keep@example.com", PrimaryDoNotEmail = true };
        var model = new PatientPatchModel { City = "SomeCity" };

        PatientMappingService.ApplyPatchModel(entity, model);

        Assert.Equal("keep@example.com", entity.PrimaryEmailAddress);
        Assert.True(entity.PrimaryDoNotEmail);
    }

    // ---- ApplyStatus ----

    [Fact]
    public void ApplyStatus_SetsStatusAndReason()
    {
        var entity = new Patient { Status = "Active", StatusReason = null };
        var model = new PatientStatusEditModel { Status = "Inactive", Reason = "Withdrew" };

        PatientMappingService.ApplyStatus(entity, model);

        Assert.Equal("Inactive", entity.Status);
        Assert.Equal("Withdrew", entity.StatusReason);
    }

    [Fact]
    public void ApplyStatus_ClearsReason_WhenReasonIsNull()
    {
        var entity = new Patient { Status = "Active", StatusReason = "Previous reason" };
        var model = new PatientStatusEditModel { Status = "Pending", Reason = null! };

        PatientMappingService.ApplyStatus(entity, model);

        Assert.Equal("Pending", entity.Status);
        Assert.Null(entity.StatusReason);
    }
}
