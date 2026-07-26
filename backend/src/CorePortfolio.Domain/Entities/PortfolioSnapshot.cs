namespace CorePortfolio.Domain.Entities;

public class PortfolioSnapshot
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
    
    public DateTime Date { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal TotalValue { get; set; }
    public decimal HoldingsValue { get; set; }
    public decimal CashValue { get; set; }
    public decimal NetAssetValue { get; set; }
    public decimal NetExternalFlow { get; set; }
    public decimal RealizedPnl { get; set; }
    public decimal UnrealizedPnl { get; set; }
    public decimal Income { get; set; }
    public decimal Fees { get; set; }
    public string BaseCurrency { get; set; } = "VND";
    public decimal UsdToVndRate { get; set; }
    public DateTime ValuationTimestamp { get; set; }
    public string QualityStatus { get; set; } = "Complete";
    public int StaleAssetCount { get; set; }
    public int UnclassifiedCashFlowCount { get; set; }
}
