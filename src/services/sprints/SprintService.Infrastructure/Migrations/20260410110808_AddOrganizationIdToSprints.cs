using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SprintService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToSprints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Sprints_ProjectId_Active",
                table: "Sprints",
                newName: "IX_Sprints_ProjectId");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Sprints",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Sprints");

            migrationBuilder.RenameIndex(
                name: "IX_Sprints_ProjectId",
                table: "Sprints",
                newName: "IX_Sprints_ProjectId_Active");
        }
    }
}
