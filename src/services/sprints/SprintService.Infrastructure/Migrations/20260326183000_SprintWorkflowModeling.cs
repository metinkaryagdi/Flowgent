using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SprintService.Infrastructure.Migrations
{
    public partial class SprintWorkflowModeling : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sprints_ProjectId",
                table: "Sprints");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Sprints",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() + INTERVAL '14 days'");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Sprints",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_ProjectId_Active",
                table: "Sprints",
                column: "ProjectId",
                unique: true,
                filter: "\"Status\" = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sprints_ProjectId_Active",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Sprints");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_ProjectId",
                table: "Sprints",
                column: "ProjectId");
        }
    }
}
