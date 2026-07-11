using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class AddIsWarrantyToRadioRepairJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWarranty",
                table: "RadioRepairJobs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWarranty",
                table: "RadioRepairJobs");
        }
    }
}
