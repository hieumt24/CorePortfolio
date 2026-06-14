namespace CorePortfolio.Domain.Entities;

public class StockInstrument
{
    public string Symbol { get; set; } = string.Empty;
    public string MarketId { get; set; } = string.Empty;
    public string SecurityGroupId { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> IndexName { get; set; } = new();
}
