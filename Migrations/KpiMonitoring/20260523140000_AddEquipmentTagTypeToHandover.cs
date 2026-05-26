using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class AddEquipmentTagTypeToHandover : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AfReading",
                table: "RadioHandovers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DisplayCondition",
                table: "RadioHandovers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "EquipmentTagType",
                table: "RadioHandovers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FrequencyError",
                table: "RadioHandovers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OriginFrom",
                table: "RadioHandovers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhysicalCondition",
                table: "RadioHandovers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PowerReading",
                table: "RadioHandovers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RepairDataDescription",
                table: "RadioHandovers",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RepairedByName",
                table: "RadioHandovers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VoltageOutNoLoad",
                table: "RadioHandovers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VoltageOutWithLoad",
                table: "RadioHandovers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AfReading", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "DisplayCondition", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "EquipmentTagType", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "FrequencyError", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "OriginFrom", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "PhysicalCondition", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "PowerReading", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "RepairDataDescription", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "RepairedByName", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "VoltageOutNoLoad", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "VoltageOutWithLoad", table: "RadioHandovers");
        }
    }
}
