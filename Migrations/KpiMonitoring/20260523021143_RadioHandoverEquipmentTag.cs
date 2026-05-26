using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class RadioHandoverEquipmentTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EquipmentName",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UnitNumber",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EquipmentName",
                table: "RadioHandovers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RadioOwnerLabel",
                table: "RadioHandovers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UnitNumber",
                table: "RadioHandovers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EquipmentName", table: "RadioRepairJobs");
            migrationBuilder.DropColumn(name: "UnitNumber", table: "RadioRepairJobs");
            migrationBuilder.DropColumn(name: "EquipmentName", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "RadioOwnerLabel", table: "RadioHandovers");
            migrationBuilder.DropColumn(name: "UnitNumber", table: "RadioHandovers");
        }
    }
}
