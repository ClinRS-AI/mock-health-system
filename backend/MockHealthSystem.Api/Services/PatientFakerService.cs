using System.Text.Json;
using Bogus;
using MockHealthSystem.Infrastructure.Data.Entities;

namespace MockHealthSystem.Api.Services;

/// <summary>
/// Generates realistic fake patient records using the Bogus faker library.
/// Most fields are populated; a few optional fields are left null/blank occasionally
/// to ensure systems handle missing data appropriately.
/// </summary>
public sealed class PatientFakerService
{
    private const string EmailDomain = "example.com";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly string[] GenderCodes = { "M", "F", "U", "O" };
    private static readonly string[] Races = { "White", "Black or African American", "Asian", "American Indian or Alaska Native", "Native Hawaiian or Other Pacific Islander", "Two or More Races", "Unknown" };
    private static readonly string[] Ethnicities = { "Hispanic or Latino", "Not Hispanic or Latino", "Unknown" };
    private static readonly string[] MaritalStatuses = { "Single", "Married", "Divorced", "Widowed", "Domestic Partner", "Unknown" };
    private static readonly string[] Languages = { "English", "Spanish", "Chinese", "Vietnamese", "Tagalog", "French", "Korean", "German", "Arabic", "Russian" };
    private static readonly string[] Titles = { "Mr.", "Mrs.", "Ms.", "Dr.", "Prof." };

    private readonly Faker _faker;
    private readonly IReadOnlyList<int> _siteIds;

    public PatientFakerService(int? seed, IReadOnlyList<int> siteIds)
    {
        _siteIds = siteIds ?? Array.Empty<int>();
        if (seed.HasValue)
            Randomizer.Seed = new Random(seed.Value);
        _faker = new Faker("en_US");
    }

    /// <summary>
    /// Creates a single patient with faker-generated demographics and identifiers.
    /// FirstName, LastName, and PrimaryEmailAddress are always set.
    /// </summary>
    public Patient CreatePatient()
    {
        var firstName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var primaryEmail = $"{_faker.Internet.UserName(firstName, lastName).ToLowerInvariant()}@{EmailDomain}";

        var patient = new Patient
        {
            PrimarySiteId = _siteIds.Count > 0 ? _siteIds[_faker.Random.Int(0, _siteIds.Count - 1)] : null,
            Status = "Active",
            FirstName = firstName,
            LastName = lastName,
            DisplayName = $"{lastName}, {firstName}",
            PrimaryEmailAddress = primaryEmail,
            PrimaryDoNotEmail = _faker.Random.Bool(0.05f),
            SecondaryDoNotEmail = false,
            DoNotMail = _faker.Random.Bool(0.05f),
            RecruitmentTextOptIn = _faker.Random.Bool(0.15f),
            ManagedMedicare = _faker.Random.Bool(0.1f),
            Caregiver = _faker.Random.Bool(0.08f),

            DateOfBirth = _faker.Date.Between(DateTime.UtcNow.AddYears(-80), DateTime.UtcNow.AddYears(-18)),
            GenderCode = _faker.PickRandom(GenderCodes),
            Race = _faker.PickRandom(Races),
            Ethnicity = _faker.PickRandom(Ethnicities),
            MaritalStatus = _faker.PickRandom(MaritalStatuses),
            NativeLanguage = _faker.PickRandom(Languages),

            Country = "US",
            Address1 = _faker.Address.StreetAddress(),
            City = _faker.Address.City(),
            State = _faker.Address.StateAbbr(),
            Zip = _faker.Address.ZipCode("#####"),

            Mrn = _faker.Random.AlphaNumeric(8).ToUpperInvariant(),
            Ssn = _faker.Random.Replace("###-##-####"),
            Uid = _faker.Random.Guid(),
            ImportSourceId = _faker.Random.Bool(0.3f) ? _faker.Random.AlphaNumeric(10) : null,
            ImportPatientId = _faker.Random.Bool(0.25f) ? _faker.Random.AlphaNumeric(12) : null,

            MiddleName = _faker.Random.Bool(0.4f) ? _faker.Name.FirstName() : null,
            PreferredName = _faker.Random.Bool(0.2f) ? _faker.Name.FirstName() : null,
            PhoneticName = _faker.Random.Bool(0.1f) ? _faker.Name.FullName() : null,
            Title = _faker.Random.Bool(0.15f) ? _faker.PickRandom(Titles) : null,

            Address2 = _faker.Random.Bool(0.25f) ? _faker.Address.SecondaryAddress() : null,
            Address3 = _faker.Random.Bool(0.05f) ? _faker.Address.BuildingNumber() : null,
            Fax = _faker.Random.Bool(0.4f) ? _faker.Phone.PhoneNumber("##########") : null,
            SecondaryEmailAddress = _faker.Random.Bool(0.5f) ? $"{_faker.Internet.UserName()}@{EmailDomain}" : null,
            PhoneTypeToText = _faker.Random.Bool(0.7f) ? _faker.PickRandom("Mobile", "Home", "Work", "Phone1", "Phone2") : null,

            WeightValue = _faker.Random.Bool(0.6f) ? Math.Round(_faker.Random.Double(45, 180), 1) : null,
            WeightUnit = _faker.Random.Bool(0.6f) ? "kg" : null,
            HeightValue = _faker.Random.Bool(0.5f) ? Math.Round(_faker.Random.Double(1.4, 2.1), 2) : null,
            HeightUnit = _faker.Random.Bool(0.5f) ? "m" : null,

            GuardianJson = _faker.Random.Bool(0.25f) ? SerializeGuardian() : null,
            PrimaryInsuranceJson = _faker.Random.Bool(0.6f) ? SerializePrimaryInsurance() : null,
            SecondaryInsuranceJson = _faker.Random.Bool(0.2f) ? SerializeSecondaryInsurance() : null,
            CustomFieldsJson = _faker.Random.Bool(0.3f) ? SerializeCustomFields() : null
        };

        return patient;
    }

    /// <summary>
    /// Creates multiple base patients. Each has required and optional fields populated per faker rules.
    /// </summary>
    public IReadOnlyList<Patient> CreatePatients(int count)
    {
        var list = new List<Patient>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(CreatePatient());
        }
        return list;
    }

    private string SerializeGuardian()
    {
        var guardian = new
        {
            firstName = _faker.Name.FirstName(),
            lastName = _faker.Name.LastName(),
            relationship = _faker.PickRandom("Parent", "Spouse", "Sibling", "Other"),
            phone = _faker.Phone.PhoneNumber("##########")
        };
        return JsonSerializer.Serialize(guardian, JsonOptions);
    }

    private string SerializePrimaryInsurance()
    {
        var insurance = new
        {
            payerName = _faker.Company.CompanyName(),
            memberId = _faker.Random.AlphaNumeric(10).ToUpperInvariant(),
            groupNumber = _faker.Random.AlphaNumeric(8)
        };
        return JsonSerializer.Serialize(insurance, JsonOptions);
    }

    private string SerializeSecondaryInsurance()
    {
        var insurance = new
        {
            payerName = _faker.Company.CompanyName(),
            memberId = _faker.Random.AlphaNumeric(10).ToUpperInvariant()
        };
        return JsonSerializer.Serialize(insurance, JsonOptions);
    }

    private string SerializeCustomFields()
    {
        var count = _faker.Random.Int(1, 3);
        var fields = new List<object>();
        for (var i = 0; i < count; i++)
        {
            fields.Add(new
            {
                name = _faker.Lorem.Word(),
                value = _faker.Lorem.Sentence()
            });
        }
        return JsonSerializer.Serialize(fields, JsonOptions);
    }
}
