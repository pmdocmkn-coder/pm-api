using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class AddRepairJobCustomStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomStatusId",
                table: "RadioRepairJobs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RepairJobCustomStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairJobCustomStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairJobCustomStatuses_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_CustomStatusId",
                table: "RadioRepairJobs",
                column: "CustomStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobCustomStatuses_CreatedByUserId",
                table: "RepairJobCustomStatuses",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RadioRepairJobs_RepairJobCustomStatuses_CustomStatusId",
                table: "RadioRepairJobs",
                column: "CustomStatusId",
                principalTable: "RepairJobCustomStatuses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RadioRepairJobs_RepairJobCustomStatuses_CustomStatusId",
                table: "RadioRepairJobs");

            migrationBuilder.DropTable(
                name: "RepairJobCustomStatuses");

            migrationBuilder.DropIndex(
                name: "IX_RadioRepairJobs_CustomStatusId",
                table: "RadioRepairJobs");

            migrationBuilder.DropColumn(
                name: "CustomStatusId",
                table: "RadioRepairJobs");
        }
    }
}
