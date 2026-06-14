namespace CorePortfolio.Domain.Entities;

public class PortfolioSnapshot
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
    
    public DateTime Date { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal TotalValue { get; set; }
}
