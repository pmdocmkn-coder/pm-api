using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class AddPmScheduleCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "PmScheduleTasks",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedByUserId",
                table: "PmScheduleTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "PmScheduleTasks",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "PmScheduleTasks",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PmScheduleTasks_CompletedByUserId",
                table: "PmScheduleTasks",
                column: "CompletedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PmScheduleTasks_Users_CompletedByUserId",
                table: "PmScheduleTasks",
                column: "CompletedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PmScheduleTasks_Users_CompletedByUserId",
                table: "PmScheduleTasks");

            migrationBuilder.DropIndex(
                name: "IX_PmScheduleTasks_CompletedByUserId",
                table: "PmScheduleTasks");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "PmScheduleTasks");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                table: "PmScheduleTasks");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "PmScheduleTasks");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "PmScheduleTasks");
        }
    }
}
