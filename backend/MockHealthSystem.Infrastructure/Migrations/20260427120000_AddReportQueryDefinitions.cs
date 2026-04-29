using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MockHealthSystem.Infrastructure.Data;

#nullable disable

namespace MockHealthSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260427120000_AddReportQueryDefinitions")]
    public partial class AddReportQueryDefinitions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportQueryDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SqlQuery = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportQueryDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportQueryDefinitions_PKey",
                table: "ReportQueryDefinitions",
                column: "PKey",
                unique: true);

            migrationBuilder.InsertData(
                table: "ReportQueryDefinitions",
                columnTypes: new[] { "integer", "character varying(128)", "text", "timestamp with time zone", "timestamp with time zone" },
                columns: new[] { "Id", "PKey", "SqlQuery", "CreatedAtUtc", "UpdatedAtUtc" },
                values: new object[,]
                {
                    {
                        1,
                        "PATIENT_COUNT",
                        "SELECT COUNT(*) AS \"PatientCount\" FROM \"Patients\"",
                        new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc)
                    },
                    {
                        2,
                        "PATIENTS_BY_STATUS",
                        "SELECT \"Status\", COUNT(*) AS \"PatientCount\" FROM \"Patients\" GROUP BY \"Status\" ORDER BY \"Status\"",
                        new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc)
                    }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportQueryDefinitions");
        }
    }
}
