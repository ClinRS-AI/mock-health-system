using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MockHealthSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    StudyId = table.Column<int>(type: "integer", nullable: false),
                    StudyArmId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubjectIdentifier = table.Column<string>(type: "text", nullable: true),
                    EnrollmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScreeningDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithdrawalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "text", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subjects_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subjects_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subjects_StudyArms_StudyArmId",
                        column: x => x.StudyArmId,
                        principalTable: "StudyArms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SubjectStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    StatusName = table.Column<string>(type: "text", nullable: false),
                    ChangedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByStaffId = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectStatuses_Staff_ChangedByStaffId",
                        column: x => x.ChangedByStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SubjectStatuses_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_PatientId",
                table: "Subjects",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_StudyArmId",
                table: "Subjects",
                column: "StudyArmId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_StudyId",
                table: "Subjects",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Uid",
                table: "Subjects",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectStatuses_ChangedByStaffId",
                table: "SubjectStatuses",
                column: "ChangedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectStatuses_SubjectId",
                table: "SubjectStatuses",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubjectStatuses");

            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
