using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CallRecords",
                columns: table => new
                {
                    CallRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CallDate = table.Column<DateTime>(type: "date", nullable: false),
                    CallTime = table.Column<TimeSpan>(type: "time(0)", nullable: false),
                    CallCloseReason = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallRecords", x => x.CallRecordId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CallSummaries",
                columns: table => new
                {
                    CallSummaryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SummaryDate = table.Column<DateTime>(type: "date", nullable: false),
                    HourGroup = table.Column<int>(type: "int", nullable: false),
                    TotalQty = table.Column<int>(type: "int", nullable: false),
                    TEBusyCount = table.Column<int>(type: "int", nullable: false),
                    SysBusyCount = table.Column<int>(type: "int", nullable: false),
                    OthersCount = table.Column<int>(type: "int", nullable: false),
                    TEBusyPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SysBusyPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OthersPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallSummaries", x => x.CallSummaryId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CctvKpcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Low")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Camera = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpCamera = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Brand = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExplicitLocation = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FotoKoordinat = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CctvKpcs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FileImportHistories",
                columns: table => new
                {
                    ImportHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileImportHistories", x => x.ImportHistoryId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FleetStatistics",
                columns: table => new
                {
                    FleetStatisticId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CallDate = table.Column<DateTime>(type: "date", nullable: false),
                    CallerFleet = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CalledFleet = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CallCount = table.Column<int>(type: "int", nullable: false),
                    TotalDuration = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetStatistics", x => x.FleetStatisticId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InternalLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LinkName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LinkGroup = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Device = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsedFrequency = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RslNearEnd = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ServiceType = table.Column<string>(type: "varchar(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalLinks", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "KpiDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PeriodMonth = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AreaGroup = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataSource = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GroupTag = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateReceived = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateSubmittedToReviewer = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateApproved = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateSubmittedToRqm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Remarks = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemarksSubmittedToReviewer = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemarksApproved = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemarksSubmittedToRqm = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiDocuments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NecTowers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NecTowers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RecipientUserId = table.Column<int>(type: "int", nullable: true),
                    RecipientRoleName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LinkUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    ReferenceType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    GroupName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PicName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PicPhone = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
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
                name: "OperationalDocumentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalDocumentTypes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PermissionName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Group = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PmSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PmSites", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Radios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Department = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Division = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Company = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Channel = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tanggal = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NomorAset = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomorUnit = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomorLv = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsTrunking = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsConventional = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Fleet = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsScrap = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ScrapJobNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateScrapped = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Remarks = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mark = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Radios", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SwrSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwrSites", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarehousePartCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartName = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehousePartCatalog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InternalLinkHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InternalLinkId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RslNearEnd = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Uptime = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScreenshotBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalLinkHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternalLinkHistories_InternalLinks_InternalLinkId",
                        column: x => x.InternalLinkId,
                        principalTable: "InternalLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NecLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LinkName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NearEndTowerId = table.Column<int>(type: "int", nullable: false),
                    FarEndTowerId = table.Column<int>(type: "int", nullable: false),
                    ExpectedRslMin = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ExpectedRslMax = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NecLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NecLinks_NecTowers_FarEndTowerId",
                        column: x => x.FarEndTowerId,
                        principalTable: "NecTowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NecLinks_NecTowers_NearEndTowerId",
                        column: x => x.NearEndTowerId,
                        principalTable: "NecTowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "PmSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Year = table.Column<int>(type: "int", nullable: false),
                    PmSiteId = table.Column<int>(type: "int", nullable: false),
                    DeviceName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PmSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PmSchedules_PmSites_PmSiteId",
                        column: x => x.PmSiteId,
                        principalTable: "PmSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RadioId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Details = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioHistories_Radios_RadioId",
                        column: x => x.RadioId,
                        principalTable: "Radios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RolePermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.RolePermissionId);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FullName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhotoUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Division = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SwrChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChannelName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SwrSiteId = table.Column<int>(type: "int", nullable: false),
                    ExpectedSwrMax = table.Column<decimal>(type: "decimal(4,2)", nullable: false, defaultValue: 1.5m),
                    ExpectedPwrMax = table.Column<decimal>(type: "decimal(6,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwrChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SwrChannels_SwrSites_SwrSiteId",
                        column: x => x.SwrSiteId,
                        principalTable: "SwrSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NecRslHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NecLinkId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RslNearEnd = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    RslFarEnd = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PmScheduleTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PmScheduleId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Week = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PmScheduleTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PmScheduleTasks_PmSchedules_PmScheduleId",
                        column: x => x.PmScheduleId,
                        principalTable: "PmSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Module = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Companies_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Divisions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Divisions_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTypes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentTypes_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Gatepasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FormattedNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Destination = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PicName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PicContact = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GatepassDate = table.Column<DateTime>(type: "date", nullable: false),
                    SignatureQRCode = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SignedByUserId = table.Column<int>(type: "int", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    VerificationToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gatepasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gatepasses_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gatepasses_Users_SignedByUserId",
                        column: x => x.SignedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gatepasses_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InspeksiTemuanKpcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ruang = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Temuan = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KategoriTemuan = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Inspector = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severity = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Medium")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TanggalTemuan = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NoFollowUp = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PerbaikanDilakukan = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TanggalPerbaikan = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TanggalSelesaiPerbaikan = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PicPelaksana = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Open")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TanggalTargetSelesai = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TanggalClosed = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Keterangan = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FotoTemuanUrls = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FotoHasilUrls = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspeksiTemuanKpcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspeksiTemuanKpcs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InspeksiTemuanKpcs_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InspeksiTemuanKpcs_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                name: "RepairJobCustomStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairJobCustomStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairJobCustomStatuses_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorkshopTechnicians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopTechnicians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkshopTechnicians_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                })
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

            migrationBuilder.CreateTable(
                name: "Quotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FormattedNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuotationDate = table.Column<DateTime>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    Nominal = table.Column<decimal>(type: "decimal(65,30)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotations_Companies_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quotations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quotations_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LetterNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FormattedNumber = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    LetterDate = table.Column<DateTime>(type: "date", nullable: false),
                    Subject = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Recipient = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttachmentUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "UTC_TIMESTAMP()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetterNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LetterNumbers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LetterNumbers_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LetterNumbers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LetterNumbers_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GatepassItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GatepassId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Unit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "unit")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GatepassItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GatepassItems_Gatepasses_GatepassId",
                        column: x => x.GatepassId,
                        principalTable: "Gatepasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Remarks = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
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
                name: "RadioRepairJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    JobNumber = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HelpdeskTicketNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioId = table.Column<int>(type: "int", nullable: true),
                    RadioSerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BatterySerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioOwnerLabel = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerDivision = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerDepartment = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DamageDescription = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentTagType = table.Column<int>(type: "int", nullable: true),
                    OriginFrom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RepairDataDescription = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RepairedByName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FrequencyError = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AfReading = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PowerReading = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VoltageOutNoLoad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VoltageOutWithLoad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhysicalCondition = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayCondition = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedTechnicianUserId = table.Column<int>(type: "int", nullable: false),
                    OpenedByUserId = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WorkshopTechnicianId = table.Column<int>(type: "int", nullable: true),
                    CurrentHandoverId = table.Column<int>(type: "int", nullable: true),
                    CustomStatusId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true),
                    AccumulatedProgressDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CurrentProgressStartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioRepairJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobs_Radios_RadioId",
                        column: x => x.RadioId,
                        principalTable: "Radios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobs_RepairJobCustomStatuses_CustomStatusId",
                        column: x => x.CustomStatusId,
                        principalTable: "RepairJobCustomStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RadioRepairJobs_Users_AssignedTechnicianUserId",
                        column: x => x.AssignedTechnicianUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobs_Users_OpenedByUserId",
                        column: x => x.OpenedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobs_WorkshopTechnicians_WorkshopTechnicianId",
                        column: x => x.WorkshopTechnicianId,
                        principalTable: "WorkshopTechnicians",
                        principalColumn: "Id");
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

            migrationBuilder.CreateTable(
                name: "RadioHandovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    HandoverNumber = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandoverType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioRepairJobId = table.Column<int>(type: "int", nullable: false),
                    RadioId = table.Column<int>(type: "int", nullable: true),
                    RadioSerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BatterySerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioOwnerLabel = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerDivision = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerDepartment = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RadioPhotoBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandedOverSignatureBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceiverSignatureBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentTagType = table.Column<int>(type: "int", nullable: false),
                    NoJobErp = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginFrom = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RepairDataDescription = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RepairedByName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FrequencyError = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AfReading = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PowerReading = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VoltageOutNoLoad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VoltageOutWithLoad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhysicalCondition = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayCondition = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandedOverByUserId = table.Column<int>(type: "int", nullable: false),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: false),
                    PicReceiverName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkshopTechnicianId = table.Column<int>(type: "int", nullable: true),
                    HandedOverByWorkshopTechnicianId = table.Column<int>(type: "int", nullable: true),
                    HandoverAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioHandovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioHandovers_RadioRepairJobs_RadioRepairJobId",
                        column: x => x.RadioRepairJobId,
                        principalTable: "RadioRepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RadioHandovers_Radios_RadioId",
                        column: x => x.RadioId,
                        principalTable: "Radios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RadioHandovers_Users_HandedOverByUserId",
                        column: x => x.HandedOverByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RadioHandovers_Users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RadioHandovers_WorkshopTechnicians_HandedOverByWorkshopTechn~",
                        column: x => x.HandedOverByWorkshopTechnicianId,
                        principalTable: "WorkshopTechnicians",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RadioHandovers_WorkshopTechnicians_WorkshopTechnicianId",
                        column: x => x.WorkshopTechnicianId,
                        principalTable: "WorkshopTechnicians",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioRepairJobStatusLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    WorkshopTechnicianName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    At = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioRepairJobStatusLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobStatusLogs_RadioRepairJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "RadioRepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RadioRepairJobStatusLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarehousePartBorrows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BorrowNumber = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BorrowedByUserId = table.Column<int>(type: "int", nullable: false),
                    BorrowerName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Purpose = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedRepairJobId = table.Column<int>(type: "int", nullable: true),
                    TicketNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApprovalNote = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RejectedByUserId = table.Column<int>(type: "int", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RejectionReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IssuedByUserId = table.Column<int>(type: "int", nullable: true),
                    IssuerSignatureBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceiverSignatureBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReturnIssuerSignatureBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReturnReceiverSignatureBase64 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReturnedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReturnCondition = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReturnNote = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReturnedByName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehousePartBorrows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehousePartBorrows_RadioRepairJobs_RelatedRepairJobId",
                        column: x => x.RelatedRepairJobId,
                        principalTable: "RadioRepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarehousePartBorrows_Users_BorrowedByUserId",
                        column: x => x.BorrowedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RadioHandoverAccessories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RadioHandoverId = table.Column<int>(type: "int", nullable: false),
                    AccessoryCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioHandoverAccessories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioHandoverAccessories_RadioHandovers_RadioHandoverId",
                        column: x => x.RadioHandoverId,
                        principalTable: "RadioHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
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

            migrationBuilder.CreateTable(
                name: "WarehousePartBorrowItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BorrowId = table.Column<int>(type: "int", nullable: false),
                    PartDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehousePartBorrowItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehousePartBorrowItems_WarehousePartBorrows_BorrowId",
                        column: x => x.BorrowId,
                        principalTable: "WarehousePartBorrows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarehousePartBorrowStatusLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BorrowId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    At = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehousePartBorrowStatusLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehousePartBorrowStatusLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehousePartBorrowStatusLogs_WarehousePartBorrows_BorrowId",
                        column: x => x.BorrowId,
                        principalTable: "WarehousePartBorrows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_Module_Time",
                table: "ActivityLogs",
                columns: new[] { "Module", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_UserId",
                table: "ActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecord_CloseReason",
                table: "CallRecords",
                column: "CallCloseReason");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecord_Date",
                table: "CallRecords",
                column: "CallDate");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecord_DateTime",
                table: "CallRecords",
                columns: new[] { "CallDate", "CallTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CallSummary_DateHour",
                table: "CallSummaries",
                columns: new[] { "SummaryDate", "HourGroup" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CctvKpc_Brand",
                table: "CctvKpcs",
                column: "Brand");

            migrationBuilder.CreateIndex(
                name: "IX_CctvKpc_IsActive",
                table: "CctvKpcs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CctvKpc_Severity",
                table: "CctvKpcs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                table: "Companies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CreatedBy",
                table: "Companies",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsActive",
                table: "Companies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_UpdatedBy",
                table: "Companies",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_Code",
                table: "Divisions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_CreatedBy",
                table: "Divisions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_UpdatedBy",
                table: "Divisions",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_Code",
                table: "DocumentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_CreatedBy",
                table: "DocumentTypes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_IsActive",
                table: "DocumentTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_UpdatedBy",
                table: "DocumentTypes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Gatepass_FormattedNumber",
                table: "Gatepasses",
                column: "FormattedNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Gatepass_Status",
                table: "Gatepasses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Gatepass_UniqueSequence",
                table: "Gatepasses",
                columns: new[] { "Year", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gatepass_YearMonth",
                table: "Gatepasses",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_Gatepasses_CreatedBy",
                table: "Gatepasses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Gatepasses_SignedByUserId",
                table: "Gatepasses",
                column: "SignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Gatepasses_UpdatedBy",
                table: "Gatepasses",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GatepassItem_GatepassId",
                table: "GatepassItems",
                column: "GatepassId");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpc_Deleted_Status",
                table: "InspeksiTemuanKpcs",
                columns: new[] { "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpc_Ruang",
                table: "InspeksiTemuanKpcs",
                column: "Ruang");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpc_Tanggal",
                table: "InspeksiTemuanKpcs",
                column: "TanggalTemuan");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpcs_CreatedBy",
                table: "InspeksiTemuanKpcs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpcs_DeletedBy",
                table: "InspeksiTemuanKpcs",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpcs_UpdatedBy",
                table: "InspeksiTemuanKpcs",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InternalLinkHistory_Date",
                table: "InternalLinkHistories",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_InternalLinkHistory_LinkDate",
                table: "InternalLinkHistories",
                columns: new[] { "InternalLinkId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_InternalLink_LinkName",
                table: "InternalLinks",
                column: "LinkName");

            migrationBuilder.CreateIndex(
                name: "IX_LetterNumber_FormattedNumber",
                table: "LetterNumbers",
                column: "FormattedNumber");

            migrationBuilder.CreateIndex(
                name: "IX_LetterNumber_LetterDate",
                table: "LetterNumbers",
                column: "LetterDate");

            migrationBuilder.CreateIndex(
                name: "IX_LetterNumber_Status",
                table: "LetterNumbers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LetterNumber_UniqueSequence",
                table: "LetterNumbers",
                columns: new[] { "CompanyId", "DocumentTypeId", "Year", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LetterNumber_YearMonth",
                table: "LetterNumbers",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_LetterNumbers_CreatedBy",
                table: "LetterNumbers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LetterNumbers_DocumentTypeId",
                table: "LetterNumbers",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LetterNumbers_UpdatedBy",
                table: "LetterNumbers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NecLinks_FarEndTowerId",
                table: "NecLinks",
                column: "FarEndTowerId");

            migrationBuilder.CreateIndex(
                name: "IX_NecLinks_LinkName",
                table: "NecLinks",
                column: "LinkName");

            migrationBuilder.CreateIndex(
                name: "IX_NecLinks_NearEndTowerId",
                table: "NecLinks",
                column: "NearEndTowerId");

            migrationBuilder.CreateIndex(
                name: "IX_NecRslHistory_Date",
                table: "NecRslHistories",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_NecRslHistory_LinkDate",
                table: "NecRslHistories",
                columns: new[] { "NecLinkId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NecTowers_Name",
                table: "NecTowers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientRoleName",
                table: "Notifications",
                column: "RecipientRoleName");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId",
                table: "Notifications",
                column: "RecipientUserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDocumentTypes_Name",
                table: "OperationalDocumentTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetToken_Token",
                table: "PasswordResetTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetToken_User_Used",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_Permission_PermissionName",
                table: "Permissions",
                column: "PermissionName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PmSchedules_PmSiteId",
                table: "PmSchedules",
                column: "PmSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_PmScheduleTasks_PmScheduleId",
                table: "PmScheduleTasks",
                column: "PmScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_CustomerId",
                table: "Quotations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_FormattedNumber",
                table: "Quotations",
                column: "FormattedNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_Status",
                table: "Quotations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_UniqueSequence",
                table: "Quotations",
                columns: new[] { "Year", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_YearMonth",
                table: "Quotations",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CreatedBy",
                table: "Quotations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_UpdatedBy",
                table: "Quotations",
                column: "UpdatedBy");

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
                name: "IX_RadioHandoverAccessories_RadioHandoverId",
                table: "RadioHandoverAccessories",
                column: "RadioHandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandoverPhotos_RadioHandoverId_SortOrder",
                table: "RadioHandoverPhotos",
                columns: new[] { "RadioHandoverId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_HandedOverByUserId",
                table: "RadioHandovers",
                column: "HandedOverByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_HandedOverByWorkshopTechnicianId",
                table: "RadioHandovers",
                column: "HandedOverByWorkshopTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_HandoverNumber",
                table: "RadioHandovers",
                column: "HandoverNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_IsDeleted_HandoverAt",
                table: "RadioHandovers",
                columns: new[] { "IsDeleted", "HandoverAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_RadioId",
                table: "RadioHandovers",
                column: "RadioId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_RadioRepairJobId",
                table: "RadioHandovers",
                column: "RadioRepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_ReceivedByUserId",
                table: "RadioHandovers",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHandovers_WorkshopTechnicianId",
                table: "RadioHandovers",
                column: "WorkshopTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioHistories_RadioId",
                table: "RadioHistories",
                column: "RadioId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_AssignedTechnicianUserId",
                table: "RadioRepairJobs",
                column: "AssignedTechnicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_CustomStatusId",
                table: "RadioRepairJobs",
                column: "CustomStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_IsDeleted_HelpdeskTicketNumber_RadioSerialNu~",
                table: "RadioRepairJobs",
                columns: new[] { "IsDeleted", "HelpdeskTicketNumber", "RadioSerialNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_JobNumber",
                table: "RadioRepairJobs",
                column: "JobNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_OpenedByUserId",
                table: "RadioRepairJobs",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_RadioId",
                table: "RadioRepairJobs",
                column: "RadioId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobs_WorkshopTechnicianId",
                table: "RadioRepairJobs",
                column: "WorkshopTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobStatusLogs_JobId",
                table: "RadioRepairJobStatusLogs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_RadioRepairJobStatusLogs_UserId",
                table: "RadioRepairJobStatusLogs",
                column: "UserId");

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
                column: "RadioId");

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

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobCustomStatuses_CreatedByUserId",
                table: "RepairJobCustomStatuses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SwrChannels_SwrSiteId_ChannelName",
                table: "SwrChannels",
                columns: new[] { "SwrSiteId", "ChannelName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SwrHistories_Date",
                table: "SwrHistories",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SwrHistories_SwrChannelId_Date",
                table: "SwrHistories",
                columns: new[] { "SwrChannelId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SwrSites_Name",
                table: "SwrSites",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrowItems_BorrowId",
                table: "WarehousePartBorrowItems",
                column: "BorrowId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrows_BorrowedByUserId",
                table: "WarehousePartBorrows",
                column: "BorrowedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrows_BorrowNumber",
                table: "WarehousePartBorrows",
                column: "BorrowNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrows_RelatedRepairJobId",
                table: "WarehousePartBorrows",
                column: "RelatedRepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrowStatusLogs_BorrowId",
                table: "WarehousePartBorrowStatusLogs",
                column: "BorrowId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePartBorrowStatusLogs_UserId",
                table: "WarehousePartBorrowStatusLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopTechnicians_UserId",
                table: "WorkshopTechnicians",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "CallRecords");

            migrationBuilder.DropTable(
                name: "CallSummaries");

            migrationBuilder.DropTable(
                name: "CctvKpcs");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.DropTable(
                name: "FileImportHistories");

            migrationBuilder.DropTable(
                name: "FleetStatistics");

            migrationBuilder.DropTable(
                name: "GatepassItems");

            migrationBuilder.DropTable(
                name: "InspeksiTemuanKpcs");

            migrationBuilder.DropTable(
                name: "InternalLinkHistories");

            migrationBuilder.DropTable(
                name: "KpiDocuments");

            migrationBuilder.DropTable(
                name: "LetterNumbers");

            migrationBuilder.DropTable(
                name: "NecRslHistories");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OperationalDocumentNotificationHistories");

            migrationBuilder.DropTable(
                name: "OperationalDocumentTypes");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "PmScheduleTasks");

            migrationBuilder.DropTable(
                name: "Quotations");

            migrationBuilder.DropTable(
                name: "RadioConventionalHistories");

            migrationBuilder.DropTable(
                name: "RadioHandoverAccessories");

            migrationBuilder.DropTable(
                name: "RadioHandoverPhotos");

            migrationBuilder.DropTable(
                name: "RadioHistories");

            migrationBuilder.DropTable(
                name: "RadioRepairJobStatusLogs");

            migrationBuilder.DropTable(
                name: "RadioScraps");

            migrationBuilder.DropTable(
                name: "RadioTrunkingHistories");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SwrHistories");

            migrationBuilder.DropTable(
                name: "WarehousePartBorrowItems");

            migrationBuilder.DropTable(
                name: "WarehousePartBorrowStatusLogs");

            migrationBuilder.DropTable(
                name: "WarehousePartCatalog");

            migrationBuilder.DropTable(
                name: "Gatepasses");

            migrationBuilder.DropTable(
                name: "InternalLinks");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "NecLinks");

            migrationBuilder.DropTable(
                name: "OperationalDocuments");

            migrationBuilder.DropTable(
                name: "PmSchedules");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "RadioHandovers");

            migrationBuilder.DropTable(
                name: "RadioConventionals");

            migrationBuilder.DropTable(
                name: "RadioTrunkings");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "SwrChannels");

            migrationBuilder.DropTable(
                name: "WarehousePartBorrows");

            migrationBuilder.DropTable(
                name: "NecTowers");

            migrationBuilder.DropTable(
                name: "PmSites");

            migrationBuilder.DropTable(
                name: "RadioGrafirs");

            migrationBuilder.DropTable(
                name: "SwrSites");

            migrationBuilder.DropTable(
                name: "RadioRepairJobs");

            migrationBuilder.DropTable(
                name: "Radios");

            migrationBuilder.DropTable(
                name: "RepairJobCustomStatuses");

            migrationBuilder.DropTable(
                name: "WorkshopTechnicians");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
