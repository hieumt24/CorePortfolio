namespace CorePortfolio.Domain.Interfaces;

public sealed record PriceQuote(
    decimal Price,
    string Currency,
    string Source,
    DateTime AsOf,
    string Status,
    string? Error = null);

public interface IPriceProvider
{
    string Source { get; }
    Task<PriceQuote?> GetQuoteAsync(string symbolOrExternalId, string currency, CancellationToken cancellationToken = default);
}
