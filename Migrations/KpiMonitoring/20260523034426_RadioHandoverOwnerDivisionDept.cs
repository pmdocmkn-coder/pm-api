using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class RadioHandoverOwnerDivisionDept : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerDepartment",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OwnerDivision",
                table: "RadioRepairJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RadioOwnerLabel",
                table: "RadioRepairJobs",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OwnerDepartment",
                table: "RadioHandovers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OwnerDivision",
                table: "RadioHandovers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerDepartment",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "OwnerDivision",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "RadioOwnerLabel",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "OwnerDepartment",
                table: "RadioHandovers");

            migrationBuilder.DropColumn(
                name: "OwnerDivision",
                table: "RadioHandovers");
        }
    }
}
