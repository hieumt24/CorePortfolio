using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SavingGoals",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RebalanceExecutionPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MarketAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DcaPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Budgets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "Version",
                value: 1);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Action_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "Action", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ActorUserId_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "ActorUserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OccurredAt",
                table: "AuditEvents",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SavingGoals");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RebalanceExecutionPlans");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MarketAssets");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DcaPlans");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Budgets");
        }
    }
}
