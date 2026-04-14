using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Issues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "IssueBoardItems",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "IssueBoardItems");
        }
    }
}
