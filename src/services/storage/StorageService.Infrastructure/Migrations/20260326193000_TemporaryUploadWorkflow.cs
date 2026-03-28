using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageService.Infrastructure.Migrations
{
    public partial class TemporaryUploadWorkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "StoredFiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAt",
                table: "StoredFiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "StoredFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE "StoredFiles"
                SET "Status" = 1,
                    "FinalizedAt" = "UploadedAt",
                    "ExpiresAt" = NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_ExpiresAt",
                table: "StoredFiles",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_Status",
                table: "StoredFiles",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredFiles_ExpiresAt",
                table: "StoredFiles");

            migrationBuilder.DropIndex(
                name: "IX_StoredFiles_Status",
                table: "StoredFiles");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "StoredFiles");

            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "StoredFiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StoredFiles");
        }
    }
}
