using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class RadioRepairHandoverSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RadioRepairJobs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "RadioRepairJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RadioRepairJobs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RadioHandovers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "RadioHandovers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RadioHandovers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_IsDeleted_HelpdeskTicketNumber_RadioSerialNumber",
                table: "RadioRepairJobs",
                columns: new[] { "IsDeleted", "HelpdeskTicketNumber", "RadioSerialNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_IsDeleted_HandoverAt",
                table: "RadioHandovers",
                columns: new[] { "IsDeleted", "HandoverAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RadioRepairJobs_IsDeleted_HelpdeskTicketNumber_RadioSerialNumber",
                table: "RadioRepairJobs");

            migrationBuilder.DropIndex(
                name: "IX_RadioHandovers_IsDeleted_HandoverAt",
                table: "RadioHandovers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RadioHandovers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RadioHandovers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RadioHandovers");
        }
    }
}
