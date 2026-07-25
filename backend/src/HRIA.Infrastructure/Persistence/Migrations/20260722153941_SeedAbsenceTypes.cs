using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRIA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAbsenceTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AbsenceTypes",
                columns: new[] { "Id", "Code", "ColorHex", "ConsumesVacationBalance", "CreatedAt", "IsActive", "Name", "RequiresApproval", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "VACACIONES", "#16b98a", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Vacaciones", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "ENFERMEDAD", "#f43f5e", false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Baja por enfermedad", false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "ASUNTOS_PROPIOS", "#f59e0b", false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Asuntos propios", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "PERMISO", "#3b82f6", false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Permiso retribuido", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "SIN_SUELDO", "#94a3b8", false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Permiso sin sueldo", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AbsenceTypes",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
