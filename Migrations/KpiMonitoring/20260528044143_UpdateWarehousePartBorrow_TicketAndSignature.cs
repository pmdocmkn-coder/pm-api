using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class UpdateWarehousePartBorrow_TicketAndSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IssuerSignatureBase64",
                table: "WarehousePartBorrows",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverSignatureBase64",
                table: "WarehousePartBorrows",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TicketNumber",
                table: "WarehousePartBorrows",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssuerSignatureBase64",
                table: "WarehousePartBorrows");

            migrationBuilder.DropColumn(
                name: "ReceiverSignatureBase64",
                table: "WarehousePartBorrows");

            migrationBuilder.DropColumn(
                name: "TicketNumber",
                table: "WarehousePartBorrows");
        }
    }
}
