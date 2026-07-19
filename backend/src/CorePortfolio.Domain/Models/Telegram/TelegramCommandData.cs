namespace CorePortfolio.Domain.Models.Telegram;

public class CashflowCommandData
{
    public decimal Amount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool ExpenseOnly { get; set; }
}

public class TransactionCommandData
{
    public int Type { get; set; } // 1: Buy, 2: Sell
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
}
