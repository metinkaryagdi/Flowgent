using System;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueService.Infrastructure.Migrations
{
    [DbContext(typeof(IssueDbContext))]
    [Migration("20260326170000_RemoveIssueSprintWriteState")]
    public partial class RemoveIssueSprintWriteState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Issues_SprintId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "SprintId",
                table: "Issues");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SprintId",
                table: "Issues",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_SprintId",
                table: "Issues",
                column: "SprintId");
        }
    }
}
