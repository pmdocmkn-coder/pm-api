using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class AddUnitToWarehousePartBorrowItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "WarehousePartBorrowItems",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "WarehousePartBorrowItems");
        }
    }
}
