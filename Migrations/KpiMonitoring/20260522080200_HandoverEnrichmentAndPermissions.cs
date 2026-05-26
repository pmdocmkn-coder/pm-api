using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations.KpiMonitoring
{
    /// <inheritdoc />
    public partial class HandoverEnrichmentAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AccessoryCode",
                table: "RadioHandoverAccessories",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RadioHandoverAccessories",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "RadioHandoverAccessories",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "RadioHandoverAccessories",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "RadioHandoverAccessories",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "RadioHandoverAccessories",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioHandoverPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RadioHandoverId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PhotoBase64 = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioHandoverPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioHandoverPhotos_RadioHandovers_RadioHandoverId",
                        column: x => x.RadioHandoverId,
                        principalTable: "RadioHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandoverPhotos_RadioHandoverId_SortOrder",
                table: "RadioHandoverPhotos",
                columns: new[] { "RadioHandoverId", "SortOrder" });

            migrationBuilder.Sql(
                "UPDATE RadioHandoverAccessories SET ItemName = AccessoryCode WHERE (ItemName = '' OR ItemName IS NULL) AND AccessoryCode IS NOT NULL");

            migrationBuilder.Sql(
                @"INSERT INTO RadioHandoverPhotos (RadioHandoverId, SortOrder, PhotoBase64)
                  SELECT Id, 0, RadioPhotoBase64 FROM RadioHandovers
                  WHERE RadioPhotoBase64 IS NOT NULL AND CHAR_LENGTH(RadioPhotoBase64) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RadioHandoverPhotos");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RadioHandoverAccessories");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "RadioHandoverAccessories");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RadioHandoverAccessories");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "RadioHandoverAccessories");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "RadioHandoverAccessories");

            migrationBuilder.UpdateData(
                table: "RadioHandoverAccessories",
                keyColumn: "AccessoryCode",
                keyValue: null,
                column: "AccessoryCode",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "AccessoryCode",
                table: "RadioHandoverAccessories",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
