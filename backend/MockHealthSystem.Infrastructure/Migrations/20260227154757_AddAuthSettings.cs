using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MockHealthSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BearerToken = table.Column<string>(type: "text", nullable: true),
                    OAuthClientId = table.Column<string>(type: "text", nullable: true),
                    OAuthClientSecret = table.Column<string>(type: "text", nullable: true),
                    AccessTokenLifetimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    RefreshTokenLifetimeDays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "text", nullable: false),
                    TokenType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthTokens", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AuthSettings",
                columns: new[] { "Id", "AccessTokenLifetimeMinutes", "BearerToken", "Mode", "OAuthClientId", "OAuthClientSecret", "RefreshTokenLifetimeDays" },
                values: new object[] { 1, 60, null, "None", null, null, 30 });

            migrationBuilder.CreateIndex(
                name: "IX_AuthTokens_Token",
                table: "AuthTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthSettings");

            migrationBuilder.DropTable(
                name: "AuthTokens");
        }
    }
}
