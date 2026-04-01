using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOutboxSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ProcessedOn", table: "OutboxMessages");
            migrationBuilder.DropColumn(name: "Error", table: "OutboxMessages");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorId",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedOn",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptedAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockId",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedUntil",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CorrelationId", table: "OutboxMessages");
            migrationBuilder.DropColumn(name: "ActorId", table: "OutboxMessages");
            migrationBuilder.DropColumn(name: "LastError", table: "OutboxMessages");
            migrationBuilder.DropColumn(name: "PublishedOn", table: "OutboxMessages");
            migrationBuilder.DropColumn(name: "LastAttemptedAt", table: "OutboxMessages");
            migrationBuilder.DropColumn(name: "LockId", table: "OutboxMessages");
            migrationBuilder.DropColumn(name: "ClaimedUntil", table: "OutboxMessages");
            migrationBuilder.DropColumn(name: "NextRetryAt", table: "OutboxMessages");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedOn",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "OutboxMessages",
                type: "text",
                nullable: true);
        }
    }
}
