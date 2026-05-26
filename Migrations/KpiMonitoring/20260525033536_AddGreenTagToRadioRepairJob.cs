using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class AddGreenTagToRadioRepairJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AfReading",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DisplayCondition",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "EquipmentTagType",
                table: "RadioRepairJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrequencyError",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OriginFrom",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhysicalCondition",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PowerReading",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RepairDataDescription",
                table: "RadioRepairJobs",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RepairedByName",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VoltageOutNoLoad",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VoltageOutWithLoad",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfReading",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "DisplayCondition",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "EquipmentTagType",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "FrequencyError",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "OriginFrom",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "PhysicalCondition",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "PowerReading",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "RepairDataDescription",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "RepairedByName",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "VoltageOutNoLoad",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "VoltageOutWithLoad",
                table: "RadioRepairJobs");
        }
    }
}
