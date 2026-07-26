namespace CorePortfolio.Domain.Entities;

public class CashLedgerEntry
{
    public Guid Id { get; set; }
    public Guid CashAccountId { get; set; }
    public CashAccount CashAccount { get; set; } = null!;
    public decimal Amount { get; set; }
    public CashLedgerEntryType Type { get; set; }
    public CashLedgerEntryClassification Classification { get; set; } = CashLedgerEntryClassification.Unknown;
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public Guid? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public Guid? CashflowRecordId { get; set; }
    public CashflowRecord? CashflowRecord { get; set; }
}

public enum CashLedgerEntryType
{
    OpeningBalance = 0,
    Buy = 1,
    Sell = 2,
    Dividend = 3,
    Deposit = 4,
    Withdrawal = 5,
    Cashflow = 6,
    MigratedOpeningBalance = 7,
    Earn = 8
}

public enum CashLedgerEntryClassification
{
    Unknown = 0,
    Contribution = 1,
    Withdrawal = 2,
    AssetPurchase = 3,
    AssetSale = 4,
    Dividend = 5,
    Fee = 6,
    Adjustment = 7,
    OpeningBalance = 8
}
