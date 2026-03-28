using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_IssueAttachments_IssueId_FileId",
                table: "IssueAttachments",
                columns: new[] { "IssueId", "FileId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IssueAttachments_IssueId_FileId",
                table: "IssueAttachments");
        }
    }
}
