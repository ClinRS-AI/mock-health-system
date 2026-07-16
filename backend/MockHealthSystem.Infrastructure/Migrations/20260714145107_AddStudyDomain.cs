using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MockHealthSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sponsors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sponsors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BackColor = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnrollmentPermitted = table.Column<bool>(type: "boolean", nullable: false),
                    StudyPhase = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ForeColor = table.Column<string>(type: "text", nullable: true),
                    BackColor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SponsorDivisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SponsorId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorDivisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorDivisions_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudySubcategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudyCategoryId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySubcategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudySubcategories_StudyCategories_StudyCategoryId",
                        column: x => x.StudyCategoryId,
                        principalTable: "StudyCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SponsorTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SponsorDivisionId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorTeams_SponsorDivisions_SponsorDivisionId",
                        column: x => x.SponsorDivisionId,
                        principalTable: "SponsorDivisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Studies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Identifier = table.Column<string>(type: "text", nullable: true),
                    ProtocolNumber = table.Column<string>(type: "text", nullable: true),
                    IndIdeNumber = table.Column<string>(type: "text", nullable: true),
                    NctNumber = table.Column<string>(type: "text", nullable: true),
                    Phase = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Subcategory = table.Column<string>(type: "text", nullable: true),
                    StudyGroup = table.Column<string>(type: "text", nullable: true),
                    Tag1 = table.Column<string>(type: "text", nullable: true),
                    Tag2 = table.Column<string>(type: "text", nullable: true),
                    Tag3 = table.Column<string>(type: "text", nullable: true),
                    Tag4 = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LaunchYear = table.Column<int>(type: "integer", nullable: true),
                    StudyCurrency = table.Column<string>(type: "text", nullable: true),
                    SponsorTeamId = table.Column<int>(type: "integer", nullable: false),
                    ManagingSiteId = table.Column<int>(type: "integer", nullable: true),
                    FinanceType = table.Column<string>(type: "text", nullable: true),
                    AccountingCode1 = table.Column<string>(type: "text", nullable: true),
                    AccountingCode2 = table.Column<string>(type: "text", nullable: true),
                    AccountingCode3 = table.Column<string>(type: "text", nullable: true),
                    AccountingCode4 = table.Column<string>(type: "text", nullable: true),
                    OpportunityLevel = table.Column<string>(type: "text", nullable: true),
                    OpportunityProbability = table.Column<double>(type: "double precision", nullable: true),
                    OpportunityExpectedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpportunityExpectedNumberOfSites = table.Column<int>(type: "integer", nullable: true),
                    OpportunityComment = table.Column<string>(type: "text", nullable: true),
                    EnrollmentNote = table.Column<string>(type: "text", nullable: true),
                    BudgetNote = table.Column<string>(type: "text", nullable: true),
                    RegulatoryNote = table.Column<string>(type: "text", nullable: true),
                    ContractNote = table.Column<string>(type: "text", nullable: true),
                    LeadSourceStaffId = table.Column<int>(type: "integer", nullable: true),
                    LeadSource = table.Column<string>(type: "text", nullable: true),
                    LeadDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LeadComment = table.Column<string>(type: "text", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Studies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Studies_Sites_ManagingSiteId",
                        column: x => x.ManagingSiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Studies_SponsorTeams_SponsorTeamId",
                        column: x => x.SponsorTeamId,
                        principalTable: "SponsorTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Studies_Staff_LeadSourceStaffId",
                        column: x => x.LeadSourceStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProtocolVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    VersionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TreatmentStatus = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    ProtocolNumber = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    IrbApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPatientReconsentRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ImportId = table.Column<string>(type: "text", nullable: true),
                    ImportType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProtocolVersions_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    ContactType = table.Column<string>(type: "text", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyContacts_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyCustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    FieldName = table.Column<string>(type: "text", nullable: false),
                    FieldValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyCustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyCustomFieldValues_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    TypeName = table.Column<string>(type: "text", nullable: true),
                    TypeCategory = table.Column<string>(type: "text", nullable: true),
                    StatusName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyDocuments_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyLeaderships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    StaffId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyLeaderships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyLeaderships_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudyLeaderships_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Importance = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    AssignedToStaffId = table.Column<int>(type: "integer", nullable: true),
                    AssignedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProjectedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HasAutoExpenditure = table.Column<bool>(type: "boolean", nullable: false),
                    SchedulingMode = table.Column<string>(type: "text", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Offset = table.Column<int>(type: "integer", nullable: true),
                    OffsetUnits = table.Column<string>(type: "text", nullable: true),
                    WindowMin = table.Column<int>(type: "integer", nullable: true),
                    WindowMax = table.Column<int>(type: "integer", nullable: true),
                    WindowUnits = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyMilestones_Staff_AssignedToStaffId",
                        column: x => x.AssignedToStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudyMilestones_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    StaffId = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedStaffId = table.Column<int>(type: "integer", nullable: true),
                    NoteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    Locked = table.Column<bool>(type: "boolean", nullable: false),
                    Shared = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyNotes_Staff_LastUpdatedStaffId",
                        column: x => x.LastUpdatedStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudyNotes_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudyNotes_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsCoordinator = table.Column<bool>(type: "boolean", nullable: false),
                    AllowRoleSharing = table.Column<bool>(type: "boolean", nullable: false),
                    RestrictReassignment = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyRoles_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyStudyTypes",
                columns: table => new
                {
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    StudyTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyStudyTypes", x => new { x.StudyId, x.StudyTypeId });
                    table.ForeignKey(
                        name: "FK_StudyStudyTypes_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyStudyTypes_StudyTypes_StudyTypeId",
                        column: x => x.StudyTypeId,
                        principalTable: "StudyTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudyTargetDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Tooltip = table.Column<string>(type: "text", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyTargetDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyTargetDates_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyArms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    ProtocolVersionId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true),
                    PatientGoal = table.Column<int>(type: "integer", nullable: true),
                    PatientLimit = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ImportId = table.Column<string>(type: "text", nullable: true),
                    ImportType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyArms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyArms_ProtocolVersions_ProtocolVersionId",
                        column: x => x.ProtocolVersionId,
                        principalTable: "ProtocolVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyArms_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    ProtocolVersionId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    OptionalProcedure = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StandardMinutes = table.Column<int>(type: "integer", nullable: true),
                    Budget = table.Column<decimal>(type: "numeric", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric", nullable: true),
                    IsBudgetAutoRecomputed = table.Column<bool>(type: "boolean", nullable: false),
                    IsCostAutoRecomputed = table.Column<bool>(type: "boolean", nullable: false),
                    PatientStipend = table.Column<decimal>(type: "numeric", nullable: true),
                    CaregiverStipend = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AutoRepeat = table.Column<bool>(type: "boolean", nullable: false),
                    RepeatOnDemand = table.Column<bool>(type: "boolean", nullable: false),
                    ImportId = table.Column<string>(type: "text", nullable: true),
                    ImportType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyVisits_ProtocolVersions_ProtocolVersionId",
                        column: x => x.ProtocolVersionId,
                        principalTable: "ProtocolVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyVisits_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyDocumentStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudyDocumentId = table.Column<int>(type: "integer", nullable: false),
                    StatusName = table.Column<string>(type: "text", nullable: false),
                    ChangedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByStaffId = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyDocumentStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyDocumentStatusHistories_Staff_ChangedByStaffId",
                        column: x => x.ChangedByStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudyDocumentStatusHistories_StudyDocuments_StudyDocumentId",
                        column: x => x.StudyDocumentId,
                        principalTable: "StudyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyRoleStaffs",
                columns: table => new
                {
                    StudyRoleId = table.Column<int>(type: "integer", nullable: false),
                    StaffId = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyRoleStaffs", x => new { x.StudyRoleId, x.StaffId });
                    table.ForeignKey(
                        name: "FK_StudyRoleStaffs_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyRoleStaffs_StudyRoles_StudyRoleId",
                        column: x => x.StudyRoleId,
                        principalTable: "StudyRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyVisitArms",
                columns: table => new
                {
                    VisitId = table.Column<int>(type: "integer", nullable: false),
                    ArmId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyVisitArms", x => new { x.VisitId, x.ArmId });
                    table.ForeignKey(
                        name: "FK_StudyVisitArms_StudyArms_ArmId",
                        column: x => x.ArmId,
                        principalTable: "StudyArms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyVisitArms_StudyVisits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "StudyVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolVersions_StudyId",
                table: "ProtocolVersions",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolVersions_Uid",
                table: "ProtocolVersions",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorDivisions_SponsorId",
                table: "SponsorDivisions",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_Uid",
                table: "Sponsors",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorTeams_SponsorDivisionId",
                table: "SponsorTeams",
                column: "SponsorDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_LeadSourceStaffId",
                table: "Studies",
                column: "LeadSourceStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_ManagingSiteId",
                table: "Studies",
                column: "ManagingSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_SponsorTeamId",
                table: "Studies",
                column: "SponsorTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_Uid",
                table: "Studies",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyArms_ProtocolVersionId",
                table: "StudyArms",
                column: "ProtocolVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyArms_StudyId",
                table: "StudyArms",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyArms_Uid",
                table: "StudyArms",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyCategories_Name",
                table: "StudyCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyContacts_StudyId_ContactType_Slot",
                table: "StudyContacts",
                columns: new[] { "StudyId", "ContactType", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyCustomFieldValues_StudyId",
                table: "StudyCustomFieldValues",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyDocuments_StudyId",
                table: "StudyDocuments",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyDocuments_Uid",
                table: "StudyDocuments",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyDocumentStatusHistories_ChangedByStaffId",
                table: "StudyDocumentStatusHistories",
                column: "ChangedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyDocumentStatusHistories_StudyDocumentId",
                table: "StudyDocumentStatusHistories",
                column: "StudyDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroups_Name",
                table: "StudyGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyLeaderships_StaffId",
                table: "StudyLeaderships",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyLeaderships_StudyId",
                table: "StudyLeaderships",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyMilestones_AssignedToStaffId",
                table: "StudyMilestones",
                column: "AssignedToStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyMilestones_StudyId",
                table: "StudyMilestones",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_LastUpdatedStaffId",
                table: "StudyNotes",
                column: "LastUpdatedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_StaffId",
                table: "StudyNotes",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_StudyId",
                table: "StudyNotes",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyRoles_StudyId",
                table: "StudyRoles",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyRoleStaffs_StaffId",
                table: "StudyRoleStaffs",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyStatusTypes_Name",
                table: "StudyStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyStudyTypes_StudyTypeId",
                table: "StudyStudyTypes",
                column: "StudyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySubcategories_StudyCategoryId",
                table: "StudySubcategories",
                column: "StudyCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyTargetDates_StudyId",
                table: "StudyTargetDates",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyTypes_Name",
                table: "StudyTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyVisitArms_ArmId",
                table: "StudyVisitArms",
                column: "ArmId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyVisits_ProtocolVersionId",
                table: "StudyVisits",
                column: "ProtocolVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyVisits_StudyId",
                table: "StudyVisits",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyVisits_Uid",
                table: "StudyVisits",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudyContacts");

            migrationBuilder.DropTable(
                name: "StudyCustomFieldValues");

            migrationBuilder.DropTable(
                name: "StudyDocumentStatusHistories");

            migrationBuilder.DropTable(
                name: "StudyGroups");

            migrationBuilder.DropTable(
                name: "StudyLeaderships");

            migrationBuilder.DropTable(
                name: "StudyMilestones");

            migrationBuilder.DropTable(
                name: "StudyNotes");

            migrationBuilder.DropTable(
                name: "StudyRoleStaffs");

            migrationBuilder.DropTable(
                name: "StudyStatusTypes");

            migrationBuilder.DropTable(
                name: "StudyStudyTypes");

            migrationBuilder.DropTable(
                name: "StudySubcategories");

            migrationBuilder.DropTable(
                name: "StudyTargetDates");

            migrationBuilder.DropTable(
                name: "StudyVisitArms");

            migrationBuilder.DropTable(
                name: "StudyDocuments");

            migrationBuilder.DropTable(
                name: "StudyRoles");

            migrationBuilder.DropTable(
                name: "StudyTypes");

            migrationBuilder.DropTable(
                name: "StudyCategories");

            migrationBuilder.DropTable(
                name: "StudyArms");

            migrationBuilder.DropTable(
                name: "StudyVisits");

            migrationBuilder.DropTable(
                name: "ProtocolVersions");

            migrationBuilder.DropTable(
                name: "Studies");

            migrationBuilder.DropTable(
                name: "SponsorTeams");

            migrationBuilder.DropTable(
                name: "SponsorDivisions");

            migrationBuilder.DropTable(
                name: "Sponsors");
        }
    }
}
