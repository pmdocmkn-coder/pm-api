using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class AddRadioManagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FormattedNumber",
                table: "LetterNumbers",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioGrafirs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NoAsset = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypeRadio = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Div = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Dept = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FleetId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tanggal = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Active")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioGrafirs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioGrafirs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_RadioGrafirs_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioConventionals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UnitNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Dept = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fleet = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Frequency = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Active")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GrafirId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioConventionals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioConventionals_RadioGrafirs_GrafirId",
                        column: x => x.GrafirId,
                        principalTable: "RadioGrafirs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RadioConventionals_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_RadioConventionals_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioTrunkings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UnitNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Dept = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fleet = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateProgram = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RadioType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JobNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Active")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Initiator = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Firmware = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChannelApply = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GrafirId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioTrunkings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioTrunkings_RadioGrafirs_GrafirId",
                        column: x => x.GrafirId,
                        principalTable: "RadioGrafirs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RadioTrunkings_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_RadioTrunkings_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioConventionalHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RadioConventionalId = table.Column<int>(type: "int", nullable: false),
                    PreviousUnitNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousDept = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousFleet = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewUnitNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewDept = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewFleet = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangeType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    ChangedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioConventionalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioConventionalHistories_RadioConventionals_RadioConventio~",
                        column: x => x.RadioConventionalId,
                        principalTable: "RadioConventionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RadioConventionalHistories_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioScraps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ScrapCategory = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypeRadio = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JobNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateScrap = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Remarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceTrunkingId = table.Column<int>(type: "int", nullable: true),
                    SourceConventionalId = table.Column<int>(type: "int", nullable: true),
                    SourceGrafirId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioScraps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioScraps_RadioConventionals_SourceConventionalId",
                        column: x => x.SourceConventionalId,
                        principalTable: "RadioConventionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RadioScraps_RadioGrafirs_SourceGrafirId",
                        column: x => x.SourceGrafirId,
                        principalTable: "RadioGrafirs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RadioScraps_RadioTrunkings_SourceTrunkingId",
                        column: x => x.SourceTrunkingId,
                        principalTable: "RadioTrunkings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RadioScraps_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioTrunkingHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RadioTrunkingId = table.Column<int>(type: "int", nullable: false),
                    PreviousUnitNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousDept = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousFleet = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewUnitNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewDept = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewFleet = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangeType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    ChangedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioTrunkingHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioTrunkingHistories_RadioTrunkings_RadioTrunkingId",
                        column: x => x.RadioTrunkingId,
                        principalTable: "RadioTrunkings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RadioTrunkingHistories_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RadioConventionalHistories_ChangedBy",
                table: "RadioConventionalHistories",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RadioConventionalHistory_ChangedAt",
                table: "RadioConventionalHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RadioConventionalHistory_RadioId",
                table: "RadioConventionalHistories",
                column: "RadioConventionalId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioConventional_RadioId",
                table: "RadioConventionals",
                column: "RadioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadioConventional_UnitNumber",
                table: "RadioConventionals",
                column: "UnitNumber");

            migrationBuilder.CreateIndex(
                name: "IX_RadioConventionals_CreatedBy",
                table: "RadioConventionals",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RadioConventionals_GrafirId",
                table: "RadioConventionals",
                column: "GrafirId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioConventionals_UpdatedBy",
                table: "RadioConventionals",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RadioGrafir_NoAsset",
                table: "RadioGrafirs",
                column: "NoAsset",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadioGrafir_SerialNumber",
                table: "RadioGrafirs",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadioGrafirs_CreatedBy",
                table: "RadioGrafirs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RadioGrafirs_UpdatedBy",
                table: "RadioGrafirs",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RadioScrap_Category",
                table: "RadioScraps",
                column: "ScrapCategory");

            migrationBuilder.CreateIndex(
                name: "IX_RadioScrap_Category_Date",
                table: "RadioScraps",
                columns: new[] { "ScrapCategory", "DateScrap" });

            migrationBuilder.CreateIndex(
                name: "IX_RadioScrap_DateScrap",
                table: "RadioScraps",
                column: "DateScrap");

            migrationBuilder.CreateIndex(
                name: "IX_RadioScraps_CreatedBy",
                table: "RadioScraps",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RadioScraps_SourceConventionalId",
                table: "RadioScraps",
                column: "SourceConventionalId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioScraps_SourceGrafirId",
                table: "RadioScraps",
                column: "SourceGrafirId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioScraps_SourceTrunkingId",
                table: "RadioScraps",
                column: "SourceTrunkingId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunkingHistories_ChangedBy",
                table: "RadioTrunkingHistories",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunkingHistory_ChangedAt",
                table: "RadioTrunkingHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunkingHistory_RadioId",
                table: "RadioTrunkingHistories",
                column: "RadioTrunkingId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunking_RadioId",
                table: "RadioTrunkings",
                column: "RadioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunking_SerialNumber",
                table: "RadioTrunkings",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunking_UnitNumber",
                table: "RadioTrunkings",
                column: "UnitNumber");

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunkings_CreatedBy",
                table: "RadioTrunkings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunkings_GrafirId",
                table: "RadioTrunkings",
                column: "GrafirId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioTrunkings_UpdatedBy",
                table: "RadioTrunkings",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RadioConventionalHistories");

            migrationBuilder.DropTable(
                name: "RadioScraps");

            migrationBuilder.DropTable(
                name: "RadioTrunkingHistories");

            migrationBuilder.DropTable(
                name: "RadioConventionals");

            migrationBuilder.DropTable(
                name: "RadioTrunkings");

            migrationBuilder.DropTable(
                name: "RadioGrafirs");

            migrationBuilder.AlterColumn<string>(
                name: "FormattedNumber",
                table: "LetterNumbers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
