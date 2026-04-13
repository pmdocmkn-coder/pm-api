using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class AddStepNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RemarksApproved",
                table: "KpiDocuments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RemarksSubmittedToReviewer",
                table: "KpiDocuments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RemarksSubmittedToRqm",
                table: "KpiDocuments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemarksApproved",
                table: "KpiDocuments");

            migrationBuilder.DropColumn(
                name: "RemarksSubmittedToReviewer",
                table: "KpiDocuments");

            migrationBuilder.DropColumn(
                name: "RemarksSubmittedToRqm",
                table: "KpiDocuments");
        }
    }
}
