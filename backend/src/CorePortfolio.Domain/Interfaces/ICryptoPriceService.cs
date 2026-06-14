namespace CorePortfolio.Domain.Interfaces;

public interface ICryptoPriceService
{
    Task<decimal?> GetPriceAsync(string coinId, CancellationToken cancellationToken = default);
}
