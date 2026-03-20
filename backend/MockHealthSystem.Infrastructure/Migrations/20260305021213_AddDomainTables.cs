using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MockHealthSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Lookup / independent tables first
            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Sites", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ProviderTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ProviderTypes", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Conditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Icd10Code = table.Column<string>(type: "text", nullable: true),
                    Icd9Code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Conditions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ConditionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ConditionTypes", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Allergies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Allergies", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Devices", x => x.Id));

            migrationBuilder.CreateTable(
                name: "MedicationRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_MedicationRoutes", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Medications", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ImmunizationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ImmunizationTypes", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Immunizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Immunizations", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Procedures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CptCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Procedures", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Relations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Relations", x => x.Id));

            migrationBuilder.CreateTable(
                name: "SocialHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialHistories_ConditionTypes_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ConditionTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SocialHistories_CategoryId",
                table: "SocialHistories",
                column: "CategoryId");

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderName = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    MiddleName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    ProviderTypeId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Providers_ProviderTypes_ProviderTypeId",
                        column: x => x.ProviderTypeId,
                        principalTable: "ProviderTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ProviderTypeId",
                table: "Providers",
                column: "ProviderTypeId");

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Address1 = table.Column<string>(type: "text", nullable: true),
                    Address2 = table.Column<string>(type: "text", nullable: true),
                    Address3 = table.Column<string>(type: "text", nullable: true),
                    Caregiver = table.Column<bool>(type: "boolean", nullable: false),
                    CaregiverId = table.Column<int>(type: "integer", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    CustomFieldsJson = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateOfDeath = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    DoNotMail = table.Column<bool>(type: "boolean", nullable: false),
                    Ethnicity = table.Column<string>(type: "text", nullable: true),
                    Fax = table.Column<string>(type: "text", nullable: true),
                    GenderCode = table.Column<string>(type: "text", nullable: true),
                    GuardianJson = table.Column<string>(type: "text", nullable: true),
                    HeightUnit = table.Column<string>(type: "text", nullable: true),
                    HeightValue = table.Column<double>(type: "double precision", nullable: true),
                    ImportId = table.Column<long>(type: "bigint", nullable: true),
                    ImportPatientId = table.Column<string>(type: "text", nullable: true),
                    ImportSourceId = table.Column<string>(type: "text", nullable: true),
                    MaritalStatus = table.Column<string>(type: "text", nullable: true),
                    MiddleName = table.Column<string>(type: "text", nullable: true),
                    Mrn = table.Column<string>(type: "text", nullable: true),
                    NativeLanguage = table.Column<string>(type: "text", nullable: true),
                    PhoneTypeToText = table.Column<string>(type: "text", nullable: true),
                    PhoneticName = table.Column<string>(type: "text", nullable: true),
                    PreferredName = table.Column<string>(type: "text", nullable: true),
                    PrimaryDoNotEmail = table.Column<bool>(type: "boolean", nullable: false),
                    PrimaryEmailAddress = table.Column<string>(type: "text", nullable: true),
                    PrimaryInsuranceJson = table.Column<string>(type: "text", nullable: true),
                    PrimarySiteId = table.Column<int>(type: "integer", nullable: true),
                    Race = table.Column<string>(type: "text", nullable: true),
                    RecruitmentTextOptIn = table.Column<bool>(type: "boolean", nullable: false),
                    SecondaryDoNotEmail = table.Column<bool>(type: "boolean", nullable: false),
                    SecondaryEmailAddress = table.Column<string>(type: "text", nullable: true),
                    SecondaryInsuranceJson = table.Column<string>(type: "text", nullable: true),
                    Ssn = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    StatusReason = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Uid = table.Column<Guid>(type: "uuid", nullable: true),
                    WeightUnit = table.Column<string>(type: "text", nullable: true),
                    WeightValue = table.Column<double>(type: "double precision", nullable: true),
                    Zip = table.Column<string>(type: "text", nullable: true),
                    ManagedMedicare = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_Sites_PrimarySiteId",
                        column: x => x.PrimarySiteId,
                        principalTable: "Sites",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PrimarySiteId",
                table: "Patients",
                column: "PrimarySiteId");

            migrationBuilder.CreateTable(
                name: "PatientPhones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: true),
                    RawNumber = table.Column<string>(type: "text", nullable: true),
                    OutOfService = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientPhones_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientPhones_PatientId",
                table: "PatientPhones",
                column: "PatientId");

            migrationBuilder.CreateTable(
                name: "PatientAllergies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    AllergyId = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reaction = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAllergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Allergies_AllergyId",
                        column: x => x.AllergyId,
                        principalTable: "Allergies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_AllergyId",
                table: "PatientAllergies",
                column: "AllergyId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_PatientId",
                table: "PatientAllergies",
                column: "PatientId");

            migrationBuilder.CreateTable(
                name: "PatientMedicalDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedicalDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientMedicalDevices_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientMedicalDevices_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalDevices_DeviceId",
                table: "PatientMedicalDevices",
                column: "DeviceId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalDevices_PatientId",
                table: "PatientMedicalDevices",
                column: "PatientId");

            migrationBuilder.CreateTable(
                name: "PatientProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientProviders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientProviders_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientProviders_PatientId",
                table: "PatientProviders",
                column: "PatientId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientProviders_ProviderId",
                table: "PatientProviders",
                column: "ProviderId");

            migrationBuilder.CreateTable(
                name: "PatientConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    ConditionId = table.Column<int>(type: "integer", nullable: false),
                    AgeAtOnset = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientConditions_Conditions_ConditionId",
                        column: x => x.ConditionId,
                        principalTable: "Conditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientConditions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_ConditionId",
                table: "PatientConditions",
                column: "ConditionId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_PatientId",
                table: "PatientConditions",
                column: "PatientId");

            migrationBuilder.CreateTable(
                name: "PatientProcedures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    ProcedureId = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ProcedureBy = table.Column<string>(type: "text", nullable: true),
                    CptCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientProcedures_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientProcedures_Procedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "Procedures",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientProcedures_PatientId",
                table: "PatientProcedures",
                column: "PatientId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientProcedures_ProcedureId",
                table: "PatientProcedures",
                column: "ProcedureId");

            migrationBuilder.CreateTable(
                name: "PatientMedications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    MedicationId = table.Column<int>(type: "integer", nullable: false),
                    RouteId = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Dosage = table.Column<string>(type: "text", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientMedications_MedicationRoutes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "MedicationRoutes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_MedicationId",
                table: "PatientMedications",
                column: "MedicationId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_PatientId",
                table: "PatientMedications",
                column: "PatientId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_RouteId",
                table: "PatientMedications",
                column: "RouteId");

            migrationBuilder.CreateTable(
                name: "PatientImmunizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    ImmunizationId = table.Column<int>(type: "integer", nullable: false),
                    ImmunizationTypeId = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientImmunizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientImmunizations_Immunizations_ImmunizationId",
                        column: x => x.ImmunizationId,
                        principalTable: "Immunizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientImmunizations_ImmunizationTypes_ImmunizationTypeId",
                        column: x => x.ImmunizationTypeId,
                        principalTable: "ImmunizationTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientImmunizations_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientImmunizations_ImmunizationId",
                table: "PatientImmunizations",
                column: "ImmunizationId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientImmunizations_ImmunizationTypeId",
                table: "PatientImmunizations",
                column: "ImmunizationTypeId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientImmunizations_PatientId",
                table: "PatientImmunizations",
                column: "PatientId");

            migrationBuilder.CreateTable(
                name: "PatientFamilyHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    ConditionId = table.Column<int>(type: "integer", nullable: false),
                    FamilyMemberId = table.Column<int>(type: "integer", nullable: false),
                    AgeAtOnset = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RelationName = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientFamilyHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientFamilyHistories_Conditions_ConditionId",
                        column: x => x.ConditionId,
                        principalTable: "Conditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientFamilyHistories_Relations_FamilyMemberId",
                        column: x => x.FamilyMemberId,
                        principalTable: "Relations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientFamilyHistories_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientFamilyHistories_ConditionId",
                table: "PatientFamilyHistories",
                column: "ConditionId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientFamilyHistories_FamilyMemberId",
                table: "PatientFamilyHistories",
                column: "FamilyMemberId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientFamilyHistories_PatientId",
                table: "PatientFamilyHistories",
                column: "PatientId");

            migrationBuilder.CreateTable(
                name: "PatientSocialHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    SocialHistoryId = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientSocialHistoryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientSocialHistoryEntries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientSocialHistoryEntries_SocialHistories_SocialHistoryId",
                        column: x => x.SocialHistoryId,
                        principalTable: "SocialHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientSocialHistoryEntries_PatientId",
                table: "PatientSocialHistoryEntries",
                column: "PatientId");
            migrationBuilder.CreateIndex(
                name: "IX_PatientSocialHistoryEntries_SocialHistoryId",
                table: "PatientSocialHistoryEntries",
                column: "SocialHistoryId");

            migrationBuilder.CreateTable(
                name: "PatientMedicationConditions",
                columns: table => new
                {
                    PatientMedicationId = table.Column<int>(type: "integer", nullable: false),
                    PatientConditionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedicationConditions", x => new { x.PatientMedicationId, x.PatientConditionId });
                    table.ForeignKey(
                        name: "FK_PatientMedicationConditions_PatientConditions_PatientConditionId",
                        column: x => x.PatientConditionId,
                        principalTable: "PatientConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientMedicationConditions_PatientMedications_PatientMedicationId",
                        column: x => x.PatientMedicationId,
                        principalTable: "PatientMedications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicationConditions_PatientConditionId",
                table: "PatientMedicationConditions",
                column: "PatientConditionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PatientMedicationConditions");
            migrationBuilder.DropTable(name: "PatientSocialHistoryEntries");
            migrationBuilder.DropTable(name: "PatientFamilyHistories");
            migrationBuilder.DropTable(name: "PatientImmunizations");
            migrationBuilder.DropTable(name: "PatientMedications");
            migrationBuilder.DropTable(name: "PatientProcedures");
            migrationBuilder.DropTable(name: "PatientConditions");
            migrationBuilder.DropTable(name: "PatientProviders");
            migrationBuilder.DropTable(name: "PatientMedicalDevices");
            migrationBuilder.DropTable(name: "PatientAllergies");
            migrationBuilder.DropTable(name: "PatientPhones");
            migrationBuilder.DropTable(name: "Patients");
            migrationBuilder.DropTable(name: "Providers");
            migrationBuilder.DropTable(name: "SocialHistories");
            migrationBuilder.DropTable(name: "Relations");
            migrationBuilder.DropTable(name: "Procedures");
            migrationBuilder.DropTable(name: "Immunizations");
            migrationBuilder.DropTable(name: "ImmunizationTypes");
            migrationBuilder.DropTable(name: "Medications");
            migrationBuilder.DropTable(name: "MedicationRoutes");
            migrationBuilder.DropTable(name: "Devices");
            migrationBuilder.DropTable(name: "Allergies");
            migrationBuilder.DropTable(name: "ConditionTypes");
            migrationBuilder.DropTable(name: "Conditions");
            migrationBuilder.DropTable(name: "ProviderTypes");
            migrationBuilder.DropTable(name: "Sites");
        }
    }
}
