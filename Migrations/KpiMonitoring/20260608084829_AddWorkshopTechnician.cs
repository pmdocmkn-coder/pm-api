using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class AddWorkshopTechnician : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkshopTechnicianId",
                table: "RadioRepairJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HandedOverByWorkshopTechnicianId",
                table: "RadioHandovers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkshopTechnicianId",
                table: "RadioHandovers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkshopTechnicians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopTechnicians", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_WorkshopTechnicianId",
                table: "RadioRepairJobs",
                column: "WorkshopTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_HandedOverByWorkshopTechnicianId",
                table: "RadioHandovers",
                column: "HandedOverByWorkshopTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_WorkshopTechnicianId",
                table: "RadioHandovers",
                column: "WorkshopTechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_RadioHandovers_WorkshopTechnicians_HandedOverByWorkshopTechn~",
                table: "RadioHandovers",
                column: "HandedOverByWorkshopTechnicianId",
                principalTable: "WorkshopTechnicians",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RadioHandovers_WorkshopTechnicians_WorkshopTechnicianId",
                table: "RadioHandovers",
                column: "WorkshopTechnicianId",
                principalTable: "WorkshopTechnicians",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RadioRepairJobs_WorkshopTechnicians_WorkshopTechnicianId",
                table: "RadioRepairJobs",
                column: "WorkshopTechnicianId",
                principalTable: "WorkshopTechnicians",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RadioHandovers_WorkshopTechnicians_HandedOverByWorkshopTechn~",
                table: "RadioHandovers");

            migrationBuilder.DropForeignKey(
                name: "FK_RadioHandovers_WorkshopTechnicians_WorkshopTechnicianId",
                table: "RadioHandovers");

            migrationBuilder.DropForeignKey(
                name: "FK_RadioRepairJobs_WorkshopTechnicians_WorkshopTechnicianId",
                table: "RadioRepairJobs");

            migrationBuilder.DropTable(
                name: "WorkshopTechnicians");

            migrationBuilder.DropIndex(
                name: "IX_RadioRepairJobs_WorkshopTechnicianId",
                table: "RadioRepairJobs");

            migrationBuilder.DropIndex(
                name: "IX_RadioHandovers_HandedOverByWorkshopTechnicianId",
                table: "RadioHandovers");

            migrationBuilder.DropIndex(
                name: "IX_RadioHandovers_WorkshopTechnicianId",
                table: "RadioHandovers");

            migrationBuilder.DropColumn(
                name: "WorkshopTechnicianId",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "HandedOverByWorkshopTechnicianId",
                table: "RadioHandovers");

            migrationBuilder.DropColumn(
                name: "WorkshopTechnicianId",
                table: "RadioHandovers");
        }
    }
}
