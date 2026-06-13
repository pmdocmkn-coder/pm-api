using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class AddUserIdToWorkshopTechnicians : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "WorkshopTechnicians",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopTechnicians_UserId",
                table: "WorkshopTechnicians",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkshopTechnicians_Users_UserId",
                table: "WorkshopTechnicians",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkshopTechnicians_Users_UserId",
                table: "WorkshopTechnicians");

            migrationBuilder.DropIndex(
                name: "IX_WorkshopTechnicians_UserId",
                table: "WorkshopTechnicians");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WorkshopTechnicians");
        }
    }
}
