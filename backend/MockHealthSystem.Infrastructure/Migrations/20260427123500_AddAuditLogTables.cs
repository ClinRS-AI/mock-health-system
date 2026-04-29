using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MockHealthSystem.Infrastructure.Data;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MockHealthSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260427123500_AddAuditLogTables")]
    public partial class AddAuditLogTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntryTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntryTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StaffUid = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StaffPKey = table.Column<int>(type: "integer", nullable: true),
                    PatientPKey = table.Column<int>(type: "integer", nullable: true),
                    StudyPKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUser = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AuditEntryTypeId = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    SourceSystem = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AuditEntryTypes_AuditEntryTypeId",
                        column: x => x.AuditEntryTypeId,
                        principalTable: "AuditEntryTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Patients_PatientPKey",
                        column: x => x.PatientPKey,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Staff_StaffPKey",
                        column: x => x.StaffPKey,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntryTypes_Code",
                table: "AuditEntryTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntryTypeId",
                table: "AuditLogs",
                column: "AuditEntryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedTimeUtc",
                table: "AuditLogs",
                column: "CreatedTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PatientPKey",
                table: "AuditLogs",
                column: "PatientPKey");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_StaffPKey",
                table: "AuditLogs",
                column: "StaffPKey");

            migrationBuilder.InsertData(
                table: "AuditEntryTypes",
                columnTypes: new[] { "integer", "character varying(64)", "character varying(256)", "text" },
                columns: new[] { "Id", "Code", "DisplayName", "Description" },
                values: new object[,]
                {
                    { 1, "PATIENT_UPDATED", "Patient Updated", "Patient chart or demographics updated." },
                    { 2, "LOGIN", "Login", "Staff user logged in." },
                    { 3, "LOGOUT", "Logout", "Staff user logged out." },
                    { 4, "PATIENT_VIEWED", "Patient Viewed", "Patient chart viewed by staff." },
                    { 5, "STAFF_PROFILE_UPDATED", "Staff Profile Updated", "Staff account profile changed." },
                    { 6, "PATIENT_CREATED", "Patient Created", "A new patient record was created." },
                    { 7, "PATIENT_DELETED", "Patient Deleted", "An existing patient record was deleted." }
                });

            migrationBuilder.InsertData(
                table: "Staff",
                columnTypes: new[] { "integer", "uuid", "character varying(128)", "character varying(128)", "boolean" },
                columns: new[] { "Id", "StaffUid", "FirstName", "LastName", "IsActive" },
                values: new object[,]
                {
                    { 1, new Guid("11111111-1111-1111-1111-111111111111"), "Alex", "Morgan", true },
                    { 2, new Guid("22222222-2222-2222-2222-222222222222"), "Jamie", "Taylor", true }
                });

            migrationBuilder.InsertData(
                table: "AuditLogs",
                columnTypes: new[] { "integer", "integer", "integer", "character varying(128)", "timestamp with time zone", "character varying(256)", "integer", "text", "text" },
                columns: new[] { "Id", "StaffPKey", "PatientPKey", "StudyPKey", "CreatedTimeUtc", "CreatedByUser", "AuditEntryTypeId", "Details", "SourceSystem" },
                values: new object[,]
                {
                    {
                        1,
                        1,
                        null,
                        "STUDY-101",
                        new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc),
                        "alex.morgan",
                        2,
                        "Staff login from clinic workstation.",
                        "MockHealthSystem"
                    },
                    {
                        2,
                        1,
                        null,
                        "STUDY-101",
                        new DateTime(2026, 4, 27, 12, 5, 0, DateTimeKind.Utc),
                        "alex.morgan",
                        4,
                        "Opened patient dashboard.",
                        "MockHealthSystem"
                    },
                    {
                        3,
                        2,
                        null,
                        "STUDY-202",
                        new DateTime(2026, 4, 27, 12, 10, 0, DateTimeKind.Utc),
                        "jamie.taylor",
                        1,
                        "Updated patient preferred contact settings.",
                        "MockHealthSystem"
                    }
                });

            migrationBuilder.InsertData(
                table: "ReportQueryDefinitions",
                columnTypes: new[] { "integer", "character varying(128)", "text", "timestamp with time zone", "timestamp with time zone" },
                columns: new[] { "Id", "PKey", "SqlQuery", "CreatedAtUtc", "UpdatedAtUtc" },
                values: new object[,]
                {
                    {
                        100,
                        "AUDIT_RECENT",
                        "SELECT l.\"Id\" AS \"AuditPKey\", l.\"CreatedTimeUtc\" AS \"CreatedTimeUtc\", l.\"CreatedByUser\" AS \"CreatedByUser\", t.\"Code\" AS \"AuditTypeCode\", t.\"DisplayName\" AS \"AuditType\", s.\"FirstName\" || ' ' || s.\"LastName\" AS \"StaffName\", l.\"PatientPKey\" AS \"PatientPKey\", l.\"StudyPKey\" AS \"StudyPKey\", l.\"Details\" AS \"Details\" FROM \"AuditLogs\" AS l INNER JOIN \"AuditEntryTypes\" AS t ON l.\"AuditEntryTypeId\" = t.\"Id\" LEFT JOIN \"Staff\" AS s ON l.\"StaffPKey\" = s.\"Id\" WHERE l.\"CreatedTimeUtc\" >= (NOW() - INTERVAL '7 minutes') ORDER BY l.\"CreatedTimeUtc\" DESC",
                        new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc)
                    },
                    {
                        101,
                        "AUDIT_BY_STAFF",
                        "SELECT s.\"Id\" AS \"StaffPKey\", s.\"FirstName\" || ' ' || s.\"LastName\" AS \"StaffName\", t.\"DisplayName\" AS \"AuditType\", l.\"CreatedTimeUtc\" AS \"CreatedTimeUtc\", l.\"CreatedByUser\" AS \"CreatedByUser\", l.\"Details\" AS \"Details\" FROM \"AuditLogs\" AS l INNER JOIN \"Staff\" AS s ON l.\"StaffPKey\" = s.\"Id\" INNER JOIN \"AuditEntryTypes\" AS t ON l.\"AuditEntryTypeId\" = t.\"Id\" ORDER BY s.\"LastName\", s.\"FirstName\", l.\"CreatedTimeUtc\" DESC",
                        new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc)
                    },
                    {
                        102,
                        "AUDIT_BY_PATIENT",
                        "SELECT l.\"PatientPKey\" AS \"PatientPKey\", t.\"DisplayName\" AS \"AuditType\", l.\"CreatedTimeUtc\" AS \"CreatedTimeUtc\", l.\"CreatedByUser\" AS \"CreatedByUser\", l.\"Details\" AS \"Details\" FROM \"AuditLogs\" AS l INNER JOIN \"AuditEntryTypes\" AS t ON l.\"AuditEntryTypeId\" = t.\"Id\" WHERE l.\"PatientPKey\" IS NOT NULL ORDER BY l.\"PatientPKey\", l.\"CreatedTimeUtc\" DESC",
                        new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc)
                    }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReportQueryDefinitions",
                keyColumn: "Id",
                keyColumnType: "integer",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "ReportQueryDefinitions",
                keyColumn: "Id",
                keyColumnType: "integer",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "ReportQueryDefinitions",
                keyColumn: "Id",
                keyColumnType: "integer",
                keyValue: 102);

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "AuditEntryTypes");

            migrationBuilder.DropTable(
                name: "Staff");
        }
    }
}
