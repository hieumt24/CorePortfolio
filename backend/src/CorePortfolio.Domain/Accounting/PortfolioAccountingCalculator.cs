using CorePortfolio.Domain.Entities;

namespace CorePortfolio.Domain.Accounting;

public sealed record AssetAccountingResult(decimal Quantity, decimal CostBasis, decimal AverageCost,
    decimal CurrentValue, decimal RealizedPnl, decimal UnrealizedPnl, decimal Fees, decimal TotalBought);

public sealed record AcquisitionAccountingResult(
    Guid TransactionId,
    decimal RemainingQuantity,
    decimal RemainingCostBasis,
    decimal UnrealizedPnl,
    bool IsClosed);

public sealed record AssetAccountingBreakdown(
    AssetAccountingResult Summary,
    IReadOnlyList<AcquisitionAccountingResult> Acquisitions);

public static class PortfolioAccountingCalculator
{
    public static AssetAccountingResult Calculate(IEnumerable<Transaction> transactions, decimal currentPrice,
        bool allowUntrackedEarnedQuantity = false)
        => CalculateBreakdown(transactions, currentPrice, allowUntrackedEarnedQuantity).Summary;

    public static AssetAccountingBreakdown CalculateBreakdown(
        IEnumerable<Transaction> transactions,
        decimal currentPrice,
        bool allowUntrackedEarnedQuantity = false)
    {
        decimal quantity = 0, costBasis = 0, realizedPnl = 0, fees = 0, totalBought = 0;
        var acquisitions = new List<OpenAcquisition>();

        foreach (var transaction in transactions
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Type == TransactionType.Sell ? 1 : 0)
            .ThenBy(t => t.Id))
        {
            if (transaction.Quantity <= 0)
                throw new AccountingValidationException("Số lượng giao dịch phải lớn hơn 0.");
            if (transaction.Price < 0 || transaction.Fee < 0)
                throw new AccountingValidationException("Giá và phí giao dịch không được âm.");

            fees += transaction.Fee;
            switch (transaction.Type)
            {
                case TransactionType.Buy:
                    var purchaseCost = transaction.Quantity * transaction.Price + transaction.Fee;
                    quantity += transaction.Quantity;
                    costBasis += purchaseCost;
                    totalBought += purchaseCost;
                    acquisitions.Add(new OpenAcquisition(transaction.Id, transaction.Quantity, purchaseCost));
                    break;
                case TransactionType.Earn:
                    // Rewards increase holdings without treating their market value as invested capital.
                    // A network/platform fee is capitalized into the remaining cost basis.
                    quantity += transaction.Quantity;
                    costBasis += transaction.Fee;
                    totalBought += transaction.Fee;
                    acquisitions.Add(new OpenAcquisition(transaction.Id, transaction.Quantity, transaction.Fee));
                    break;
                case TransactionType.Sell:
                    var trackedQuantity = transaction.Quantity;
                    if (transaction.Quantity > quantity)
                    {
                        if (!allowUntrackedEarnedQuantity)
                            throw new AccountingValidationException("Không thể bán vượt quá số lượng đang sở hữu.");

                        // Imported exchange history may start after the asset was acquired. Its missing
                        // cost basis is unknown, so exclude that part from realized PnL instead of
                        // incorrectly treating the sale proceeds as pure profit.
                        trackedQuantity = quantity;
                    }
                    var averageCost = quantity == 0 ? 0 : costBasis / quantity;
                    var disposedCost = averageCost * trackedQuantity;
                    var trackedRatio = transaction.Quantity == 0 ? 0 : trackedQuantity / transaction.Quantity;
                    realizedPnl += trackedQuantity * transaction.Price - transaction.Fee * trackedRatio - disposedCost;
                    ReduceOpenAcquisitions(acquisitions, quantity, trackedQuantity);
                    quantity -= trackedQuantity;
                    costBasis -= disposedCost;
                    if (quantity == 0) costBasis = 0;
                    break;
                case TransactionType.Dividend:
                    realizedPnl += transaction.Quantity * transaction.Price - transaction.Fee;
                    break;
            }
        }

        var currentValue = quantity * currentPrice;
        var summary = new AssetAccountingResult(quantity, costBasis, quantity == 0 ? 0 : costBasis / quantity,
            currentValue, realizedPnl, currentValue - costBasis, fees, totalBought);
        var acquisitionResults = acquisitions
            .Select(acquisition => new AcquisitionAccountingResult(
                acquisition.TransactionId,
                acquisition.RemainingQuantity,
                acquisition.RemainingCostBasis,
                acquisition.RemainingQuantity * currentPrice - acquisition.RemainingCostBasis,
                acquisition.RemainingQuantity == 0))
            .ToList();

        return new AssetAccountingBreakdown(summary, acquisitionResults);
    }

    private static void ReduceOpenAcquisitions(
        IEnumerable<OpenAcquisition> acquisitions,
        decimal currentQuantity,
        decimal disposedQuantity)
    {
        if (currentQuantity <= 0 || disposedQuantity <= 0)
            return;

        var remainingRatio = (currentQuantity - disposedQuantity) / currentQuantity;
        foreach (var acquisition in acquisitions.Where(item => item.RemainingQuantity > 0))
        {
            acquisition.RemainingQuantity *= remainingRatio;
            acquisition.RemainingCostBasis *= remainingRatio;
            if (remainingRatio == 0)
            {
                acquisition.RemainingQuantity = 0;
                acquisition.RemainingCostBasis = 0;
            }
        }
    }

    private sealed class OpenAcquisition(Guid transactionId, decimal remainingQuantity, decimal remainingCostBasis)
    {
        public Guid TransactionId { get; } = transactionId;
        public decimal RemainingQuantity { get; set; } = remainingQuantity;
        public decimal RemainingCostBasis { get; set; } = remainingCostBasis;
    }
}

public sealed class AccountingValidationException : Exception
{
    public AccountingValidationException(string message) : base(message) { }
}
