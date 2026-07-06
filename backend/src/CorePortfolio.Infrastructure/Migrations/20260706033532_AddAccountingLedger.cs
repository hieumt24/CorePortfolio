using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "Transactions",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Transactions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BaseCurrency",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QualityStatus",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UsdToVndRate",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValuationTimestamp",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "CashAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashAccounts_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CashAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CashflowRecordId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashLedgerEntries_CashAccounts_CashAccountId",
                        column: x => x.CashAccountId,
                        principalTable: "CashAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CashLedgerEntries_CashflowRecords_CashflowRecordId",
                        column: x => x.CashflowRecordId,
                        principalTable: "CashflowRecords",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CashLedgerEntries_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccounts_PortfolioId_Currency",
                table: "CashAccounts",
                columns: new[] { "PortfolioId", "Currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashLedgerEntries_CashAccountId",
                table: "CashLedgerEntries",
                column: "CashAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashLedgerEntries_CashflowRecordId",
                table: "CashLedgerEntries",
                column: "CashflowRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CashLedgerEntries_TransactionId",
                table: "CashLedgerEntries",
                column: "TransactionId",
                unique: true);

            migrationBuilder.Sql("""
                UPDATE PortfolioSnapshots
                SET BaseCurrency = 'VND', QualityStatus = 'Legacy', ValuationTimestamp = Date;

                INSERT INTO CashAccounts (Id, PortfolioId, Currency)
                SELECT lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' ||
                       substr(lower(hex(randomblob(2))), 2) || '-' ||
                       substr('89ab', abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' ||
                       lower(hex(randomblob(6))), p.Id, 'VND'
                FROM Portfolios p;

                INSERT OR IGNORE INTO CashAccounts (Id, PortfolioId, Currency)
                SELECT lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' ||
                       substr(lower(hex(randomblob(2))), 2) || '-' ||
                       substr('89ab', abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' ||
                       lower(hex(randomblob(6))), t.PortfolioId, upper(ac.DefaultCurrency)
                FROM Transactions t
                JOIN Assets a ON a.Id = t.AssetId
                JOIN MarketAssets ma ON ma.Id = a.MarketAssetId
                JOIN AssetCategories ac ON ac.Id = ma.CategoryId
                GROUP BY t.PortfolioId, upper(ac.DefaultCurrency);

                INSERT INTO CashLedgerEntries
                    (Id, CashAccountId, Amount, Type, Description, OccurredAt, TransactionId, CashflowRecordId)
                SELECT lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' ||
                       substr(lower(hex(randomblob(2))), 2) || '-' ||
                       substr('89ab', abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' ||
                       lower(hex(randomblob(6))), ca.Id,
                       CASE t.Type
                           WHEN 0 THEN -(t.Quantity * t.Price + t.Fee)
                           WHEN 1 THEN t.Quantity * t.Price - t.Fee
                           WHEN 2 THEN t.Quantity * t.Price
                           WHEN 3 THEN -(t.Quantity * t.Price)
                           WHEN 4 THEN t.Quantity * t.Price - t.Fee
                       END,
                       CASE t.Type WHEN 0 THEN 1 WHEN 1 THEN 2 WHEN 2 THEN 4 WHEN 3 THEN 5 WHEN 4 THEN 3 END,
                       'Migrated transaction', t.Date, t.Id, NULL
                FROM Transactions t
                JOIN Assets a ON a.Id = t.AssetId
                JOIN MarketAssets ma ON ma.Id = a.MarketAssetId
                JOIN AssetCategories ac ON ac.Id = ma.CategoryId
                JOIN CashAccounts ca ON ca.PortfolioId = t.PortfolioId AND ca.Currency = upper(ac.DefaultCurrency);

                INSERT INTO CashLedgerEntries
                    (Id, CashAccountId, Amount, Type, Description, OccurredAt, TransactionId, CashflowRecordId)
                SELECT lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' ||
                       substr(lower(hex(randomblob(2))), 2) || '-' ||
                       substr('89ab', abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' ||
                       lower(hex(randomblob(6))), CashAccountId, -MinimumBalance, 7,
                       'Số dư đầu kỳ được tạo khi migration; vui lòng kiểm tra lại',
                       datetime(FirstEntry, '-1 second'), NULL, NULL
                FROM (
                    SELECT CashAccountId, MIN(RunningBalance) AS MinimumBalance, MIN(OccurredAt) AS FirstEntry
                    FROM (
                        SELECT CashAccountId, OccurredAt,
                               SUM(Amount) OVER (PARTITION BY CashAccountId ORDER BY OccurredAt, Id) AS RunningBalance
                        FROM CashLedgerEntries
                    ) running
                    GROUP BY CashAccountId
                ) balances
                WHERE MinimumBalance < 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashLedgerEntries");

            migrationBuilder.DropTable(
                name: "CashAccounts");

            migrationBuilder.DropColumn(
                name: "Fee",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BaseCurrency",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "UsdToVndRate",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "ValuationTimestamp",
                table: "PortfolioSnapshots");
        }
    }
}
