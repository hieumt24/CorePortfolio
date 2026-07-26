namespace CorePortfolio.Domain.Interfaces;

public interface ICryptoPriceService
{
    Task<decimal?> GetPriceAsync(string coinId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, decimal>> GetPricesAsync(IEnumerable<string> coinIds, CancellationToken cancellationToken = default);
}

public sealed record CryptoMarketInstrument(
    string ExternalId,
    string Symbol,
    string Name,
    decimal Price,
    int? MarketCapRank,
    DateTime AsOf);

public interface ICryptoMarketService
{
    Task<IReadOnlyList<CryptoMarketInstrument>> GetTopMarketsAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
