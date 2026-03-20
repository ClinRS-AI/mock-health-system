using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MockHealthSystem.Infrastructure.Data;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MockHealthSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260312090000_AddSystemReferenceMetadata")]
    public partial class AddSystemReferenceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Conditions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenderCode",
                table: "Conditions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ChildBearing",
                table: "Conditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ConditionTypeId",
                table: "Conditions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ConditionTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Medications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ChildBearing",
                table: "Medications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MedicationTypeId",
                table: "Medications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GenderId",
                table: "Medications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultRouteId",
                table: "Medications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultScheduleId",
                table: "Medications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Allergies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AllergenTypeId",
                table: "Allergies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AllergenTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AllergenTypeId = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllergenTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    GenderCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_AllergenTypeId",
                table: "Allergies",
                column: "AllergenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Conditions_ConditionTypeId",
                table: "Conditions",
                column: "ConditionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_DefaultRouteId",
                table: "Medications",
                column: "DefaultRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_DefaultScheduleId",
                table: "Medications",
                column: "DefaultScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_GenderId",
                table: "Medications",
                column: "GenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_MedicationTypeId",
                table: "Medications",
                column: "MedicationTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Allergies_AllergenTypes_AllergenTypeId",
                table: "Allergies",
                column: "AllergenTypeId",
                principalTable: "AllergenTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Conditions_ConditionTypes_ConditionTypeId",
                table: "Conditions",
                column: "ConditionTypeId",
                principalTable: "ConditionTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_Genders_GenderId",
                table: "Medications",
                column: "GenderId",
                principalTable: "Genders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_MedicationRoutes_DefaultRouteId",
                table: "Medications",
                column: "DefaultRouteId",
                principalTable: "MedicationRoutes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_MedicationSchedules_DefaultScheduleId",
                table: "Medications",
                column: "DefaultScheduleId",
                principalTable: "MedicationSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_MedicationTypes_MedicationTypeId",
                table: "Medications",
                column: "MedicationTypeId",
                principalTable: "MedicationTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allergies_AllergenTypes_AllergenTypeId",
                table: "Allergies");

            migrationBuilder.DropForeignKey(
                name: "FK_Conditions_ConditionTypes_ConditionTypeId",
                table: "Conditions");

            migrationBuilder.DropForeignKey(
                name: "FK_Medications_Genders_GenderId",
                table: "Medications");

            migrationBuilder.DropForeignKey(
                name: "FK_Medications_MedicationRoutes_DefaultRouteId",
                table: "Medications");

            migrationBuilder.DropForeignKey(
                name: "FK_Medications_MedicationSchedules_DefaultScheduleId",
                table: "Medications");

            migrationBuilder.DropForeignKey(
                name: "FK_Medications_MedicationTypes_MedicationTypeId",
                table: "Medications");

            migrationBuilder.DropTable(
                name: "AllergenTypes");

            migrationBuilder.DropTable(
                name: "Genders");

            migrationBuilder.DropTable(
                name: "MedicationSchedules");

            migrationBuilder.DropTable(
                name: "MedicationTypes");

            migrationBuilder.DropIndex(
                name: "IX_Allergies_AllergenTypeId",
                table: "Allergies");

            migrationBuilder.DropIndex(
                name: "IX_Conditions_ConditionTypeId",
                table: "Conditions");

            migrationBuilder.DropIndex(
                name: "IX_Medications_DefaultRouteId",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_DefaultScheduleId",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_GenderId",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_MedicationTypeId",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Conditions");

            migrationBuilder.DropColumn(
                name: "GenderCode",
                table: "Conditions");

            migrationBuilder.DropColumn(
                name: "ChildBearing",
                table: "Conditions");

            migrationBuilder.DropColumn(
                name: "ConditionTypeId",
                table: "Conditions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ConditionTypes");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "ChildBearing",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "MedicationTypeId",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "GenderId",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "DefaultRouteId",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "DefaultScheduleId",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Allergies");

            migrationBuilder.DropColumn(
                name: "AllergenTypeId",
                table: "Allergies");
        }
    }
}

