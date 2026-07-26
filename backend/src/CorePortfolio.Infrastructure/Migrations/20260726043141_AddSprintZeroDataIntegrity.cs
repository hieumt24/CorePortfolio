using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSprintZeroDataIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This repair previously ran on every application startup. Deferring foreign-key
            // validation keeps the one-time primary/foreign key normalization atomic.
            migrationBuilder.Sql("PRAGMA defer_foreign_keys = ON;");
            migrationBuilder.Sql("""
                UPDATE CashAccounts
                SET Id = UPPER(Id)
                WHERE Id != UPPER(Id);
                """);
            migrationBuilder.Sql("""
                UPDATE CashLedgerEntries
                SET Id = UPPER(Id),
                    CashAccountId = UPPER(CashAccountId)
                WHERE Id != UPPER(Id)
                   OR CashAccountId != UPPER(CashAccountId);
                """);

            // Preserve the most recently written snapshot before enforcing one row per day.
            migrationBuilder.Sql("""
                DELETE FROM PortfolioSnapshots
                WHERE rowid NOT IN (
                    SELECT MAX(rowid)
                    FROM PortfolioSnapshots
                    GROUP BY PortfolioId, Date
                );
                """);

            migrationBuilder.DropIndex(
                name: "IX_PortfolioSnapshots_PortfolioId",
                table: "PortfolioSnapshots");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioSnapshots_PortfolioId_Date",
                table: "PortfolioSnapshots",
                columns: new[] { "PortfolioId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PortfolioSnapshots_PortfolioId_Date",
                table: "PortfolioSnapshots");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioSnapshots_PortfolioId",
                table: "PortfolioSnapshots",
                column: "PortfolioId");
        }
    }
}
