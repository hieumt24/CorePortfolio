namespace CorePortfolio.Domain.Interfaces;

public interface ICryptoPriceService
{
    Task<decimal?> GetPriceAsync(string coinId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, decimal>> GetPricesAsync(IEnumerable<string> coinIds, CancellationToken cancellationToken = default);
}
