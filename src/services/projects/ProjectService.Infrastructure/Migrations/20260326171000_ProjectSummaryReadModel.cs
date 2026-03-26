using System;
using BitirmeProject.ProjectService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectService.Infrastructure.Migrations
{
    [DbContext(typeof(ProjectDbContext))]
    [Migration("20260326171000_ProjectSummaryReadModel")]
    public partial class ProjectSummaryReadModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoneIssueCount",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "InProgressIssueCount",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IssueCount",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OpenIssueCount",
                table: "Projects");

            migrationBuilder.CreateTable(
                name: "ProjectSummaries",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueCount = table.Column<int>(type: "integer", nullable: false),
                    OpenIssueCount = table.Column<int>(type: "integer", nullable: false),
                    InProgressIssueCount = table.Column<int>(type: "integer", nullable: false),
                    DoneIssueCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSummaries", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_ProjectSummaries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectSummaries");

            migrationBuilder.AddColumn<int>(
                name: "DoneIssueCount",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InProgressIssueCount",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IssueCount",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenIssueCount",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
