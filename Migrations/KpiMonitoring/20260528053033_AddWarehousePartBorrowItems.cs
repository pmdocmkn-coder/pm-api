using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class AddWarehousePartBorrowItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartCode",
                table: "WarehousePartBorrows");

            migrationBuilder.DropColumn(
                name: "PartDescription",
                table: "WarehousePartBorrows");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "WarehousePartBorrows");

            migrationBuilder.AddColumn<string>(
                name: "ReturnIssuerSignatureBase64",
                table: "WarehousePartBorrows",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReturnReceiverSignatureBase64",
                table: "WarehousePartBorrows",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarehousePartBorrowItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BorrowId = table.Column<int>(type: "int", nullable: false),
                    PartDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehousePartBorrowItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehousePartBorrowItems_WarehousePartBorrows_BorrowId",
                        column: x => x.BorrowId,
                        principalTable: "WarehousePartBorrows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrowItems_BorrowId",
                table: "WarehousePartBorrowItems",
                column: "BorrowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehousePartBorrowItems");

            migrationBuilder.DropColumn(
                name: "ReturnIssuerSignatureBase64",
                table: "WarehousePartBorrows");

            migrationBuilder.DropColumn(
                name: "ReturnReceiverSignatureBase64",
                table: "WarehousePartBorrows");

            migrationBuilder.AddColumn<string>(
                name: "PartCode",
                table: "WarehousePartBorrows",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PartDescription",
                table: "WarehousePartBorrows",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "WarehousePartBorrows",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
