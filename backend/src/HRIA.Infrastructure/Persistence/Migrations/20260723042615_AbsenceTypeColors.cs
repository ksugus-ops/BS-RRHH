using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRIA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AbsenceTypeColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "ColorHex",
                value: "#1baf7a");

            migrationBuilder.UpdateData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "ColorHex",
                value: "#e34948");

            migrationBuilder.UpdateData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "ColorHex",
                value: "#eda100");

            migrationBuilder.UpdateData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "ColorHex",
                value: "#2a78d6");

            migrationBuilder.UpdateData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "ColorHex",
                value: "#4a3aa7");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "ColorHex",
                value: "#16b98a");

            migrationBuilder.UpdateData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "ColorHex",
                value: "#f43f5e");

            migrationBuilder.UpdateData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "ColorHex",
                value: "#f59e0b");

            migrationBuilder.UpdateData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "ColorHex",
                value: "#3b82f6");

            migrationBuilder.UpdateData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "ColorHex",
                value: "#94a3b8");
        }
    }
}
