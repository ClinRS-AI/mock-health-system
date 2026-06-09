using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockHealthSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRateLimitColumnsToAuthSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RateLimitEnabled",
                table: "AuthSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RateLimitPerMinute",
                table: "AuthSettings",
                type: "integer",
                nullable: false,
                defaultValue: 300);

            migrationBuilder.AddColumn<int>(
                name: "RateLimitPerSecond",
                table: "AuthSettings",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.UpdateData(
                table: "AuthSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "RateLimitEnabled", "RateLimitPerMinute", "RateLimitPerSecond" },
                values: new object[] { false, 300, 10 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RateLimitEnabled",
                table: "AuthSettings");

            migrationBuilder.DropColumn(
                name: "RateLimitPerMinute",
                table: "AuthSettings");

            migrationBuilder.DropColumn(
                name: "RateLimitPerSecond",
                table: "AuthSettings");
        }
    }
}
