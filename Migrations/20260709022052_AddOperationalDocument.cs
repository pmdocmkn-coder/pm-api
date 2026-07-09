using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationalDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PicName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PicPhone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileLink = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FollowUpStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalDocuments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OperationalDocumentNotificationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OperationalDocumentId = table.Column<int>(type: "int", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    DaysRemaining = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalDocumentNotificationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationalDocumentNotificationHistories_OperationalDocument~",
                        column: x => x.OperationalDocumentId,
                        principalTable: "OperationalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDocumentNotificationHistories_DaysRemaining",
                table: "OperationalDocumentNotificationHistories",
                column: "DaysRemaining");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDocumentNotificationHistories_OperationalDocument~",
                table: "OperationalDocumentNotificationHistories",
                columns: new[] { "OperationalDocumentId", "NotifiedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDocuments_FollowUpStatus",
                table: "OperationalDocuments",
                column: "FollowUpStatus");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDocuments_Type",
                table: "OperationalDocuments",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDocuments_ValidUntil",
                table: "OperationalDocuments",
                column: "ValidUntil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationalDocumentNotificationHistories");

            migrationBuilder.DropTable(
                name: "OperationalDocuments");
        }
    }
}
