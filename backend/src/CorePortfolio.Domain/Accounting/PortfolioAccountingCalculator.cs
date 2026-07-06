using CorePortfolio.Domain.Entities;

namespace CorePortfolio.Domain.Accounting;

public sealed record AssetAccountingResult(decimal Quantity, decimal CostBasis, decimal AverageCost,
    decimal CurrentValue, decimal RealizedPnl, decimal UnrealizedPnl, decimal Fees, decimal TotalBought);

public static class PortfolioAccountingCalculator
{
    public static AssetAccountingResult Calculate(IEnumerable<Transaction> transactions, decimal currentPrice)
    {
        decimal quantity = 0, costBasis = 0, realizedPnl = 0, fees = 0, totalBought = 0;

        foreach (var transaction in transactions.OrderBy(t => t.Date).ThenBy(t => t.Id))
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
                    break;
                case TransactionType.Sell:
                    if (transaction.Quantity > quantity)
                        throw new AccountingValidationException("Không thể bán vượt quá số lượng đang sở hữu.");
                    var averageCost = quantity == 0 ? 0 : costBasis / quantity;
                    var disposedCost = averageCost * transaction.Quantity;
                    realizedPnl += transaction.Quantity * transaction.Price - transaction.Fee - disposedCost;
                    quantity -= transaction.Quantity;
                    costBasis -= disposedCost;
                    if (quantity == 0) costBasis = 0;
                    break;
                case TransactionType.Dividend:
                    realizedPnl += transaction.Quantity * transaction.Price - transaction.Fee;
                    break;
            }
        }

        var currentValue = quantity * currentPrice;
        return new AssetAccountingResult(quantity, costBasis, quantity == 0 ? 0 : costBasis / quantity,
            currentValue, realizedPnl, currentValue - costBasis, fees, totalBought);
    }
}

public sealed class AccountingValidationException : Exception
{
    public AccountingValidationException(string message) : base(message) { }
}
