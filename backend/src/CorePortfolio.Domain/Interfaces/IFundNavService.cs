namespace CorePortfolio.Domain.Interfaces;

public sealed record FundNavInstrument(
    string ExternalId,
    string Symbol,
    string Name,
    string? FundType,
    decimal Nav,
    DateTime AsOf);

public interface IFundNavService
{
    Task<IReadOnlyList<FundNavInstrument>> GetFundsAsync(
        CancellationToken cancellationToken = default);
}
