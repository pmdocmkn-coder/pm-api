using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.RadioRepairDuration
{
    /// <inheritdoc />
    public partial class AddRadioRepairDurationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccumulatedProgressDurationMinutes",
                table: "RadioRepairJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentProgressStartedAt",
                table: "RadioRepairJobs",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccumulatedProgressDurationMinutes",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "CurrentProgressStartedAt",
                table: "RadioRepairJobs");
        }
    }
}
