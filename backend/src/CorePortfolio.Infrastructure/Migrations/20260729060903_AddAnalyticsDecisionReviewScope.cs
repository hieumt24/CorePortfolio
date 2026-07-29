using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsDecisionReviewScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPortfolioScope",
                table: "AnalyticsDecisions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE "AnalyticsDecisions"
                SET "IsPortfolioScope" = 1
                WHERE "PortfolioId" IS NOT NULL
                   OR "PortfolioNameSnapshot" <> 'Tất cả danh mục';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPortfolioScope",
                table: "AnalyticsDecisions");
        }
    }
}
