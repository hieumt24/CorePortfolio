using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashLedgerEntries_CashAccountId",
                table: "CashLedgerEntries");

            migrationBuilder.AddColumn<decimal>(
                name: "CashValue",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Fees",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HoldingsValue",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Income",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAssetValue",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetExternalFlow",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RealizedPnl",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StaleAssetCount",
                table: "PortfolioSnapshots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnclassifiedCashFlowCount",
                table: "PortfolioSnapshots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnrealizedPnl",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Classification",
                table: "CashLedgerEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE CashLedgerEntries
                SET Classification = CASE Type
                    WHEN 0 THEN 8
                    WHEN 1 THEN 3
                    WHEN 2 THEN 4
                    WHEN 3 THEN 5
                    WHEN 4 THEN 1
                    WHEN 5 THEN 2
                    WHEN 8 THEN 6
                    ELSE 0
                END;

                UPDATE PortfolioSnapshots
                SET HoldingsValue = TotalValue,
                    CashValue = COALESCE((
                        SELECT SUM(
                            CASE upper(account.Currency)
                                WHEN 'USD' THEN entry.Amount * PortfolioSnapshots.UsdToVndRate
                                ELSE entry.Amount
                            END)
                        FROM CashAccounts account
                        INNER JOIN CashLedgerEntries entry
                            ON entry.CashAccountId = account.Id
                        WHERE account.PortfolioId = PortfolioSnapshots.PortfolioId
                          AND date(entry.OccurredAt) <= date(PortfolioSnapshots.Date)
                    ), 0),
                    NetExternalFlow = COALESCE((
                        SELECT SUM(
                            CASE upper(account.Currency)
                                WHEN 'USD' THEN entry.Amount * PortfolioSnapshots.UsdToVndRate
                                ELSE entry.Amount
                            END)
                        FROM CashAccounts account
                        INNER JOIN CashLedgerEntries entry
                            ON entry.CashAccountId = account.Id
                        WHERE account.PortfolioId = PortfolioSnapshots.PortfolioId
                          AND date(entry.OccurredAt) = date(PortfolioSnapshots.Date)
                          AND entry.Classification IN (1, 2, 8)
                    ), 0),
                    Income = COALESCE((
                        SELECT SUM(
                            transactionItem.Quantity * transactionItem.Price *
                            CASE upper(category.DefaultCurrency)
                                WHEN 'USD' THEN PortfolioSnapshots.UsdToVndRate
                                ELSE 1
                            END)
                        FROM Transactions transactionItem
                        INNER JOIN Assets asset
                            ON asset.Id = transactionItem.AssetId
                        INNER JOIN MarketAssets marketAsset
                            ON marketAsset.Id = asset.MarketAssetId
                        INNER JOIN AssetCategories category
                            ON category.Id = marketAsset.CategoryId
                        WHERE transactionItem.PortfolioId = PortfolioSnapshots.PortfolioId
                          AND transactionItem.Type = 4
                          AND date(transactionItem.Date) <= date(PortfolioSnapshots.Date)
                    ), 0),
                    Fees = COALESCE((
                        SELECT SUM(
                            transactionItem.Fee *
                            CASE upper(category.DefaultCurrency)
                                WHEN 'USD' THEN PortfolioSnapshots.UsdToVndRate
                                ELSE 1
                            END)
                        FROM Transactions transactionItem
                        INNER JOIN Assets asset
                            ON asset.Id = transactionItem.AssetId
                        INNER JOIN MarketAssets marketAsset
                            ON marketAsset.Id = asset.MarketAssetId
                        INNER JOIN AssetCategories category
                            ON category.Id = marketAsset.CategoryId
                        WHERE transactionItem.PortfolioId = PortfolioSnapshots.PortfolioId
                          AND date(transactionItem.Date) <= date(PortfolioSnapshots.Date)
                    ), 0),
                    UnclassifiedCashFlowCount = (
                        SELECT COUNT(*)
                        FROM CashAccounts account
                        INNER JOIN CashLedgerEntries entry
                            ON entry.CashAccountId = account.Id
                        WHERE account.PortfolioId = PortfolioSnapshots.PortfolioId
                          AND date(entry.OccurredAt) = date(PortfolioSnapshots.Date)
                          AND entry.Classification = 0
                    ),
                    QualityStatus = 'Legacy';

                UPDATE PortfolioSnapshots
                SET NetAssetValue = HoldingsValue + CashValue,
                    TotalValue = HoldingsValue + CashValue;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CashLedgerEntries_CashAccountId_Classification_OccurredAt",
                table: "CashLedgerEntries",
                columns: new[] { "CashAccountId", "Classification", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashLedgerEntries_CashAccountId_Classification_OccurredAt",
                table: "CashLedgerEntries");

            migrationBuilder.DropColumn(
                name: "CashValue",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "Fees",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "HoldingsValue",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "Income",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "NetAssetValue",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "NetExternalFlow",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "RealizedPnl",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "StaleAssetCount",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "UnclassifiedCashFlowCount",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "UnrealizedPnl",
                table: "PortfolioSnapshots");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "CashLedgerEntries");

            migrationBuilder.CreateIndex(
                name: "IX_CashLedgerEntries_CashAccountId",
                table: "CashLedgerEntries",
                column: "CashAccountId");
        }
    }
}
