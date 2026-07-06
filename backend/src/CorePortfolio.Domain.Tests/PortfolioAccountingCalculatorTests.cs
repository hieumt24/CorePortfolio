using CorePortfolio.Domain.Accounting;
using CorePortfolio.Domain.Entities;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public class PortfolioAccountingCalculatorTests
{
    [Fact]
    public void UsesWeightedAverageCostAndSeparatesRealizedFromUnrealizedPnl()
    {
        var transactions = new[]
        {
            Tx(TransactionType.Buy, 10, 100, 10, new DateTime(2026, 1, 1)),
            Tx(TransactionType.Buy, 10, 200, 20, new DateTime(2026, 1, 2)),
            Tx(TransactionType.Sell, 5, 250, 5, new DateTime(2026, 1, 3))
        };

        var result = PortfolioAccountingCalculator.Calculate(transactions, 220);

        Assert.Equal(15, result.Quantity);
        Assert.Equal(151.5m, result.AverageCost);
        Assert.Equal(2272.5m, result.CostBasis);
        Assert.Equal(487.5m, result.RealizedPnl);
        Assert.Equal(1027.5m, result.UnrealizedPnl);
        Assert.Equal(35, result.Fees);
    }

    [Fact]
    public void OrdersBackdatedTransactionsBeforeCalculating()
    {
        var transactions = new[]
        {
            Tx(TransactionType.Sell, 2, 150, 0, new DateTime(2026, 2, 2)),
            Tx(TransactionType.Buy, 3, 100, 0, new DateTime(2026, 2, 1))
        };

        var result = PortfolioAccountingCalculator.Calculate(transactions, 120);
        Assert.Equal(1, result.Quantity);
        Assert.Equal(100, result.CostBasis);
        Assert.Equal(100, result.RealizedPnl);
    }

    [Fact]
    public void IncludesDividendAndFeeInRealizedPnl()
    {
        var result = PortfolioAccountingCalculator.Calculate(new[]
        {
            Tx(TransactionType.Buy, 1, 100, 0, new DateTime(2026, 1, 1)),
            Tx(TransactionType.Dividend, 2, 10, 1, new DateTime(2026, 1, 2))
        }, 100);
        Assert.Equal(19, result.RealizedPnl);
    }

    [Fact]
    public void RejectsSellingMoreThanOwned()
    {
        var transactions = new[]
        {
            Tx(TransactionType.Buy, 1, 100, 0, new DateTime(2026, 1, 1)),
            Tx(TransactionType.Sell, 2, 100, 0, new DateTime(2026, 1, 2))
        };
        Assert.Throws<AccountingValidationException>(() => PortfolioAccountingCalculator.Calculate(transactions, 100));
    }

    private static Transaction Tx(TransactionType type, decimal quantity, decimal price, decimal fee, DateTime date) =>
        new() { Id = Guid.NewGuid(), Type = type, Quantity = quantity, Price = price, Fee = fee, Date = date };
}
