using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Infrastructure.Migrations
{
    public partial class NotificationDeliveryWorkerModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttemptCount",
                table: "Notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDeliveryAttemptAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastFailureReason",
                table: "Notifications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextDeliveryAttemptAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Notifications"
                SET "DeliveryAttemptCount" = CASE WHEN "Status" IN (1, 2, 3) THEN 1 ELSE 0 END,
                    "LastDeliveryAttemptAt" = CASE WHEN "Status" IN (1, 2, 3) THEN COALESCE("UpdatedAt", "CreatedAt") ELSE NULL END,
                    "DeliveredAt" = CASE WHEN "Status" = 3 THEN COALESCE("UpdatedAt", "CreatedAt") ELSE NULL END,
                    "NextDeliveryAttemptAt" = CASE
                        WHEN "Status" = 0 THEN COALESCE("CreatedAt", NOW())
                        WHEN "Status" = 2 THEN NOW()
                        ELSE NULL
                    END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_NextDeliveryAttemptAt",
                table: "Notifications",
                column: "NextDeliveryAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status",
                table: "Notifications",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_NextDeliveryAttemptAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Status",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeliveryAttemptCount",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "LastDeliveryAttemptAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "LastFailureReason",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NextDeliveryAttemptAt",
                table: "Notifications");
        }
    }
}
