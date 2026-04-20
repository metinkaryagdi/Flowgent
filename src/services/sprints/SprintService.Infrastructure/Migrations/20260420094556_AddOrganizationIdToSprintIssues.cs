using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SprintService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToSprintIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "SprintIssues",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SprintIssues_OrganizationId",
                table: "SprintIssues",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SprintIssues_OrganizationId",
                table: "SprintIssues");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SprintIssues");
        }
    }
}
