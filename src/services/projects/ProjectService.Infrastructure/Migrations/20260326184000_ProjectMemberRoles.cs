using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectService.Infrastructure.Migrations
{
    public partial class ProjectMemberRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "ProjectMembers",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.Sql(
                """
                INSERT INTO "ProjectMembers" ("ProjectId", "UserId", "AddedByUserId", "AddedAt", "Role")
                SELECT p."Id", p."OwnerUserId", p."OwnerUserId", COALESCE(p."UpdatedAt", p."CreatedAt"), 0
                FROM "Projects" p
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "ProjectMembers" pm
                    WHERE pm."ProjectId" = p."Id"
                      AND pm."UserId" = p."OwnerUserId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "ProjectMembers" pm
                USING "Projects" p
                WHERE pm."ProjectId" = p."Id"
                  AND pm."UserId" = p."OwnerUserId"
                  AND pm."Role" = 0;
                """);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "ProjectMembers");
        }
    }
}
