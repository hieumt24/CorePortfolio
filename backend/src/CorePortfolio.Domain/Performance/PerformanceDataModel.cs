using CorePortfolio.Domain.Entities;

namespace CorePortfolio.Domain.Performance;

public static class CashLedgerEntryClassificationRules
{
    public static CashLedgerEntryClassification Classify(CashLedgerEntryType type) => type switch
    {
        CashLedgerEntryType.OpeningBalance => CashLedgerEntryClassification.OpeningBalance,
        CashLedgerEntryType.Buy => CashLedgerEntryClassification.AssetPurchase,
        CashLedgerEntryType.Sell => CashLedgerEntryClassification.AssetSale,
        CashLedgerEntryType.Dividend => CashLedgerEntryClassification.Dividend,
        CashLedgerEntryType.Deposit => CashLedgerEntryClassification.Contribution,
        CashLedgerEntryType.Withdrawal => CashLedgerEntryClassification.Withdrawal,
        CashLedgerEntryType.Earn => CashLedgerEntryClassification.Fee,
        CashLedgerEntryType.Cashflow or CashLedgerEntryType.MigratedOpeningBalance =>
            CashLedgerEntryClassification.Unknown,
        _ => CashLedgerEntryClassification.Unknown
    };

    public static bool IsExternalFlow(this CashLedgerEntryClassification classification) =>
        classification is CashLedgerEntryClassification.Contribution
            or CashLedgerEntryClassification.Withdrawal
            or CashLedgerEntryClassification.OpeningBalance;
}

public static class PortfolioSnapshotQuality
{
    public const string Complete = "Complete";
    public const string StalePrices = "StalePrices";
    public const string Partial = "Partial";
    public const string Legacy = "Legacy";
    public const string Unavailable = "Unavailable";

    public static string Evaluate(int staleAssetCount, int unclassifiedCashFlowCount)
    {
        if (unclassifiedCashFlowCount > 0)
            return Partial;

        return staleAssetCount > 0 ? StalePrices : Complete;
    }
}
