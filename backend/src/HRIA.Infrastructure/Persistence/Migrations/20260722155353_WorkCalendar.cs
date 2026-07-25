using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRIA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Holidays_Date",
                table: "Holidays");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Holidays",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkCalendarId",
                table: "Holidays",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "WorkCalendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NonWorkingWeekDaysMask = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCalendars", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_WorkCalendarId_Date",
                table: "Holidays",
                columns: new[] { "WorkCalendarId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkCalendars_Year",
                table: "WorkCalendars",
                column: "Year",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Holidays_WorkCalendars_WorkCalendarId",
                table: "Holidays",
                column: "WorkCalendarId",
                principalTable: "WorkCalendars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Holidays_WorkCalendars_WorkCalendarId",
                table: "Holidays");

            migrationBuilder.DropTable(
                name: "WorkCalendars");

            migrationBuilder.DropIndex(
                name: "IX_Holidays_WorkCalendarId_Date",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "WorkCalendarId",
                table: "Holidays");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Date",
                table: "Holidays",
                column: "Date",
                unique: true);
        }
    }
}
