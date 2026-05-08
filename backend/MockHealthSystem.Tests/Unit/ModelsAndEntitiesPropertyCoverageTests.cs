using MockHealthSystem.Api.Models.System;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class ModelsAndEntitiesPropertyCoverageTests
{

    [Fact]
    public void SysAllergyViewModel_FromEntity_MapsAllergenType()
    {
        var entity = new Allergy
        {
            Id = 1,
            Name = "Peanut",
            Description = "Tree nut",
            AllergenType = new AllergenType
            {
                Id = 2,
                AllergenTypeId = "FOOD",
                Description = "Food",
                IsDefault = true
            }
        };

        var vm = SysAllergyViewModel.FromEntity(entity);

        Assert.Equal(1, vm.Id);
        Assert.Equal("Peanut", vm.Name);
        Assert.Equal("Tree nut", vm.Description);
        Assert.NotNull(vm.Allergen);
        Assert.Equal("FOOD", vm.Allergen!.AllergenTypeId);
        Assert.True(vm.Allergen.IsDefault);
    }

    [Fact]
    public void SysAllergyViewModel_FromEntity_HandlesNullAllergenType()
    {
        var entity = new Allergy { Id = 5, Name = "Latex" };

        var vm = SysAllergyViewModel.FromEntity(entity);

        Assert.Null(vm.Allergen);
    }

    [Fact]
    public void SysConditionViewModel_FromEntity_MapsCategory()
    {
        var entity = new Condition
        {
            Id = 1,
            Name = "Diabetes",
            Icd10Code = "E11",
            Icd9Code = "250",
            Description = "Endocrine",
            GenderCode = "U",
            ChildBearing = false,
            ConditionType = new ConditionType
            {
                Id = 2,
                Name = "Endocrine",
                Description = "Endocrine conditions"
            }
        };

        var vm = SysConditionViewModel.FromEntity(entity);

        Assert.Equal("Diabetes", vm.Name);
        Assert.NotNull(vm.Category);
        Assert.Equal("Endocrine", vm.Category!.Name);
    }

    [Fact]
    public void SysConditionViewModel_FromEntity_HandlesNullCategory()
    {
        var entity = new Condition
        {
            Id = 3,
            Name = "Hypertension"
        };

        var vm = SysConditionViewModel.FromEntity(entity);

        Assert.Null(vm.Category);
    }

    [Fact]
    public void SysMedicationViewModel_FromEntity_MapsAllNestedTypes()
    {
        var entity = new Medication
        {
            Id = 1,
            Name = "MedA",
            Description = "Description",
            ChildBearing = true,
            MedicationType = new MedicationType { Id = 2, Name = "Antibiotic" },
            Gender = new Gender { Id = 3, Name = "Female", GenderCode = "F" },
            DefaultRoute = new MedicationRoute { Id = 4, Name = "Oral" },
            DefaultSchedule = new MedicationSchedule { Id = 5, Name = "BID" }
        };

        var vm = SysMedicationViewModel.FromEntity(entity);

        Assert.Equal("MedA", vm.Name);
        Assert.NotNull(vm.Category);
        Assert.NotNull(vm.Gender);
        Assert.NotNull(vm.DefaultRoute);
        Assert.NotNull(vm.DefaultSchedule);
        Assert.Equal("Antibiotic", vm.Category!.Name);
        Assert.Equal("F", vm.Gender!.GenderCode);
        Assert.Equal("Oral", vm.DefaultRoute!.Name);
        Assert.Equal("BID", vm.DefaultSchedule!.Name);
    }

    [Fact]
    public void SysMedicationViewModel_FromEntity_HandlesAllNullsOnReferences()
    {
        var entity = new Medication { Id = 7, Name = "Plain" };

        var vm = SysMedicationViewModel.FromEntity(entity);

        Assert.Null(vm.Category);
        Assert.Null(vm.Gender);
        Assert.Null(vm.DefaultRoute);
        Assert.Null(vm.DefaultSchedule);
    }
}
