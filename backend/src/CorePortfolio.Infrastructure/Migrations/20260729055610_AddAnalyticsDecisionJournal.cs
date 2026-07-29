using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsDecisionJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalyticsDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PortfolioNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    DecisionType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Rationale = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    PlannedAction = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    RiskTriggers = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ReviewOutcome = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ReviewNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ScopeFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScopeTo = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    DataQualityStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TrackedPortfolioValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    TimeWeightedReturnPercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    MoneyWeightedReturnPercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    MaximumDrawdownPercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    InsightCodes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MethodologyVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticsDecisions_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AnalyticsDecisions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDecisions_PortfolioId",
                table: "AnalyticsDecisions",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDecisions_UserId_CreatedAt",
                table: "AnalyticsDecisions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDecisions_UserId_Status_ReviewDate",
                table: "AnalyticsDecisions",
                columns: new[] { "UserId", "Status", "ReviewDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsDecisions");
        }
    }
}
