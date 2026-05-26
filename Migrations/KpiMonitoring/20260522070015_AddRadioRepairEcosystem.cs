using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class AddRadioRepairEcosystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RadioRepairJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    JobNumber = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HelpdeskTicketNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioId = table.Column<int>(type: "int", nullable: true),
                    RadioSerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BatterySerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DamageDescription = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedTechnicianUserId = table.Column<int>(type: "int", nullable: false),
                    OpenedByUserId = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CurrentHandoverId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioRepairJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobs_Radios_RadioId",
                        column: x => x.RadioId,
                        principalTable: "Radios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobs_Users_AssignedTechnicianUserId",
                        column: x => x.AssignedTechnicianUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobs_Users_OpenedByUserId",
                        column: x => x.OpenedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioHandovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    HandoverNumber = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandoverType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioRepairJobId = table.Column<int>(type: "int", nullable: false),
                    RadioId = table.Column<int>(type: "int", nullable: true),
                    RadioSerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BatterySerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioPhotoBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandedOverSignatureBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceiverSignatureBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandedOverByUserId = table.Column<int>(type: "int", nullable: false),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: false),
                    HandoverAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioHandovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioHandovers_RadioRepairJobs_RadioRepairJobId",
                        column: x => x.RadioRepairJobId,
                        principalTable: "RadioRepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RadioHandovers_Radios_RadioId",
                        column: x => x.RadioId,
                        principalTable: "Radios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RadioHandovers_Users_HandedOverByUserId",
                        column: x => x.HandedOverByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RadioHandovers_Users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioRepairJobStatusLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    At = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioRepairJobStatusLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobStatusLogs_RadioRepairJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "RadioRepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobStatusLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarehousePartBorrows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BorrowNumber = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BorrowedByUserId = table.Column<int>(type: "int", nullable: false),
                    PartDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedRepairJobId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApprovalNote = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RejectedByUserId = table.Column<int>(type: "int", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RejectionReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IssuedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReturnCondition = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReturnNote = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehousePartBorrows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehousePartBorrows_RadioRepairJobs_RelatedRepairJobId",
                        column: x => x.RelatedRepairJobId,
                        principalTable: "RadioRepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarehousePartBorrows_Users_BorrowedByUserId",
                        column: x => x.BorrowedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioHandoverAccessories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RadioHandoverId = table.Column<int>(type: "int", nullable: false),
                    AccessoryCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioHandoverAccessories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioHandoverAccessories_RadioHandovers_RadioHandoverId",
                        column: x => x.RadioHandoverId,
                        principalTable: "RadioHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarehousePartBorrowStatusLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BorrowId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    At = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehousePartBorrowStatusLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehousePartBorrowStatusLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehousePartBorrowStatusLogs_WarehousePartBorrows_BorrowId",
                        column: x => x.BorrowId,
                        principalTable: "WarehousePartBorrows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandoverAccessories_RadioHandoverId",
                table: "RadioHandoverAccessories",
                column: "RadioHandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_HandedOverByUserId",
                table: "RadioHandovers",
                column: "HandedOverByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_HandoverNumber",
                table: "RadioHandovers",
                column: "HandoverNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_RadioId",
                table: "RadioHandovers",
                column: "RadioId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_RadioRepairJobId",
                table: "RadioHandovers",
                column: "RadioRepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_ReceivedByUserId",
                table: "RadioHandovers",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_AssignedTechnicianUserId",
                table: "RadioRepairJobs",
                column: "AssignedTechnicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_JobNumber",
                table: "RadioRepairJobs",
                column: "JobNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_OpenedByUserId",
                table: "RadioRepairJobs",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_RadioId",
                table: "RadioRepairJobs",
                column: "RadioId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobStatusLogs_JobId",
                table: "RadioRepairJobStatusLogs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobStatusLogs_UserId",
                table: "RadioRepairJobStatusLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrows_BorrowedByUserId",
                table: "WarehousePartBorrows",
                column: "BorrowedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrows_BorrowNumber",
                table: "WarehousePartBorrows",
                column: "BorrowNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrows_RelatedRepairJobId",
                table: "WarehousePartBorrows",
                column: "RelatedRepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrowStatusLogs_BorrowId",
                table: "WarehousePartBorrowStatusLogs",
                column: "BorrowId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrowStatusLogs_UserId",
                table: "WarehousePartBorrowStatusLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RadioHandoverAccessories");

            migrationBuilder.DropTable(
                name: "RadioRepairJobStatusLogs");

            migrationBuilder.DropTable(
                name: "WarehousePartBorrowStatusLogs");

            migrationBuilder.DropTable(
                name: "RadioHandovers");

            migrationBuilder.DropTable(
                name: "WarehousePartBorrows");

            migrationBuilder.DropTable(
                name: "RadioRepairJobs");
        }
    }
}
