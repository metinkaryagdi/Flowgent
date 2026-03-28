using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SprintService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSprintSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SprintSummaries",
                columns: table => new
                {
                    SprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalIssues = table.Column<int>(type: "integer", nullable: false),
                    CompletedIssues = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotTakenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SprintSummaries", x => x.SprintId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SprintSummaries_SprintId",
                table: "SprintSummaries",
                column: "SprintId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SprintSummaries");
        }
    }
}
