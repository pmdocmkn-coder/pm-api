using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class CreateNecTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NecTowers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Location = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NecTowers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NecLinks",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LinkName = table.Column<string>(maxLength: 200, nullable: false),
                    NearEndTowerId = table.Column<int>(nullable: false),
                    FarEndTowerId = table.Column<int>(nullable: false),
                    ExpectedRslMin = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ExpectedRslMax = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NecLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NecLinks_NecTowers_NearEndTowerId",
                        column: x => x.NearEndTowerId,
                        principalTable: "NecTowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NecLinks_NecTowers_FarEndTowerId",
                        column: x => x.FarEndTowerId,
                        principalTable: "NecTowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NecRslHistories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NecLinkId = table.Column<int>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false),
                    RslNearEnd = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    RslFarEnd = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NecRslHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NecRslHistories_NecLinks_NecLinkId",
                        column: x => x.NecLinkId,
                        principalTable: "NecLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NecTowers_Name",
                table: "NecTowers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NecLinks_LinkName",
                table: "NecLinks",
                column: "LinkName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NecRslHistories_NecLinkId_Date",
                table: "NecRslHistories",
                columns: new[] { "NecLinkId", "Date" },
                unique: true);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
