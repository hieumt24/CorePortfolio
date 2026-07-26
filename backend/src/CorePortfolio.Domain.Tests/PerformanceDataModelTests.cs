using CorePortfolio.Domain.Accounting;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Performance;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class PerformanceDataModelTests
{
    [Theory]
    [InlineData(CashLedgerEntryType.OpeningBalance, CashLedgerEntryClassification.OpeningBalance)]
    [InlineData(CashLedgerEntryType.Buy, CashLedgerEntryClassification.AssetPurchase)]
    [InlineData(CashLedgerEntryType.Sell, CashLedgerEntryClassification.AssetSale)]
    [InlineData(CashLedgerEntryType.Dividend, CashLedgerEntryClassification.Dividend)]
    [InlineData(CashLedgerEntryType.Deposit, CashLedgerEntryClassification.Contribution)]
    [InlineData(CashLedgerEntryType.Withdrawal, CashLedgerEntryClassification.Withdrawal)]
    [InlineData(CashLedgerEntryType.Earn, CashLedgerEntryClassification.Fee)]
    [InlineData(CashLedgerEntryType.Cashflow, CashLedgerEntryClassification.Unknown)]
    [InlineData(CashLedgerEntryType.MigratedOpeningBalance, CashLedgerEntryClassification.Unknown)]
    public void Classify_MapsOperationalLedgerTypeToPerformanceMeaning(
        CashLedgerEntryType entryType,
        CashLedgerEntryClassification expected)
    {
        Assert.Equal(expected, CashLedgerEntryClassificationRules.Classify(entryType));
    }

    [Theory]
    [InlineData(CashLedgerEntryClassification.Contribution, true)]
    [InlineData(CashLedgerEntryClassification.Withdrawal, true)]
    [InlineData(CashLedgerEntryClassification.OpeningBalance, true)]
    [InlineData(CashLedgerEntryClassification.AssetPurchase, false)]
    [InlineData(CashLedgerEntryClassification.AssetSale, false)]
    [InlineData(CashLedgerEntryClassification.Dividend, false)]
    [InlineData(CashLedgerEntryClassification.Fee, false)]
    [InlineData(CashLedgerEntryClassification.Adjustment, false)]
    [InlineData(CashLedgerEntryClassification.Unknown, false)]
    public void IsExternalFlow_OnlyIncludesCapitalEnteringOrLeavingPortfolio(
        CashLedgerEntryClassification classification,
        bool expected)
    {
        Assert.Equal(expected, classification.IsExternalFlow());
    }

    [Theory]
    [InlineData(0, 0, PortfolioSnapshotQuality.Complete)]
    [InlineData(2, 0, PortfolioSnapshotQuality.StalePrices)]
    [InlineData(0, 1, PortfolioSnapshotQuality.Partial)]
    [InlineData(2, 1, PortfolioSnapshotQuality.Partial)]
    public void SnapshotQuality_ReportsKnownDataProblems(
        int staleAssets,
        int unclassifiedCashFlows,
        string expected)
    {
        Assert.Equal(
            expected,
            PortfolioSnapshotQuality.Evaluate(staleAssets, unclassifiedCashFlows));
    }

    [Theory]
    [InlineData("Fiat")]
    [InlineData("Cash")]
    [InlineData("Tiền mặt")]
    [InlineData("Tiền pháp định")]
    public void IsFiat_MatchesEnglishAndVietnameseNames(string categoryName)
    {
        Assert.True(AssetCategoryClassifier.IsFiat(categoryName));
    }
}
