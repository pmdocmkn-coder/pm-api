using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.InternalLink
{
    /// <inheritdoc />
    public partial class RemoveRadioIdUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RadioTrunking_RadioId",
                table: "RadioTrunkings");

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunking_RadioId",
                table: "RadioTrunkings",
                column: "RadioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RadioTrunking_RadioId",
                table: "RadioTrunkings");

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunking_RadioId",
                table: "RadioTrunkings",
                column: "RadioId",
                unique: true);
        }
    }
}
