namespace CorePortfolio.Domain.Interfaces;

public interface IStockPriceService
{
    Task<decimal?> GetStockPriceAsync(string symbol, CancellationToken cancellationToken = default);
}
