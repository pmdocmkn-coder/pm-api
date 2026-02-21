using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.InternalLink
{
    /// <inheritdoc />
    public partial class AddLinkGroupAndDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "InternalLinks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LinkGroup",
                table: "InternalLinks",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direction",
                table: "InternalLinks");

            migrationBuilder.DropColumn(
                name: "LinkGroup",
                table: "InternalLinks");
        }
    }
}
