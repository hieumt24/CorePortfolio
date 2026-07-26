namespace CorePortfolio.Domain.Interfaces;

public sealed record MarketIndexQuote(
    string Symbol,
    string Name,
    decimal Value,
    decimal Change,
    decimal ChangePercent,
    DateTime AsOf,
    string Source,
    string Status,
    string? Error = null);

public interface IMarketIndexService
{
    Task<IReadOnlyList<MarketIndexQuote>> GetQuotesAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default);
}
