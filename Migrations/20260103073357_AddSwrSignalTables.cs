using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class AddSwrSignalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SwrHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SwrChannelId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Fpwr = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Vswr = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwrHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SwrHistories_SwrChannels_SwrChannelId",
                        column: x => x.SwrChannelId,
                        principalTable: "SwrChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

    

            migrationBuilder.CreateIndex(
                name: "IX_SwrHistories_Date",
                table: "SwrHistories",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SwrHistories_SwrChannelId_Date",
                table: "SwrHistories",
                columns: new[] { "SwrChannelId", "Date" },
                unique: true);

         
        
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
          

            migrationBuilder.DropTable(
                name: "SwrHistories");


            migrationBuilder.DropTable(
                name: "SwrChannels");

         
            migrationBuilder.DropTable(
                name: "NecTowers");

       
        }
    }
}
