using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.RadioRepairDuration
{
    /// <inheritdoc />
    public partial class AddNominalToQuotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Nominal",
                table: "Quotations",
                type: "decimal(65,30)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nominal",
                table: "Quotations");
        }
    }
}
