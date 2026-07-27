using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockHealthSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectCcFidelityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScreeningDate",
                table: "Subjects");

            migrationBuilder.RenameColumn(
                name: "WithdrawalReason",
                table: "Subjects",
                newName: "TreatmentStatus");

            migrationBuilder.RenameColumn(
                name: "WithdrawalDate",
                table: "Subjects",
                newName: "TreatmentStart");

            migrationBuilder.RenameColumn(
                name: "SubjectIdentifier",
                table: "Subjects",
                newName: "Tag");

            migrationBuilder.AddColumn<string>(
                name: "EnrollmentLocation",
                table: "Subjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ethnicity",
                table: "Subjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacilityCode",
                table: "Subjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenderCode",
                table: "Subjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportId",
                table: "Subjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Narrative",
                table: "Subjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProtocolVersionId",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Race",
                table: "Subjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RandomizationNumber",
                table: "Subjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreeningNumber",
                table: "Subjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_ProtocolVersionId",
                table: "Subjects",
                column: "ProtocolVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SiteId",
                table: "Subjects",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_ProtocolVersions_ProtocolVersionId",
                table: "Subjects",
                column: "ProtocolVersionId",
                principalTable: "ProtocolVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Sites_SiteId",
                table: "Subjects",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_ProtocolVersions_ProtocolVersionId",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Sites_SiteId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_ProtocolVersionId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_SiteId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "EnrollmentLocation",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Ethnicity",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "FacilityCode",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "GenderCode",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "ImportId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Narrative",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "ProtocolVersionId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Race",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "RandomizationNumber",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "ScreeningNumber",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Subjects");

            migrationBuilder.RenameColumn(
                name: "TreatmentStatus",
                table: "Subjects",
                newName: "WithdrawalReason");

            migrationBuilder.RenameColumn(
                name: "TreatmentStart",
                table: "Subjects",
                newName: "WithdrawalDate");

            migrationBuilder.RenameColumn(
                name: "Tag",
                table: "Subjects",
                newName: "SubjectIdentifier");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScreeningDate",
                table: "Subjects",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
