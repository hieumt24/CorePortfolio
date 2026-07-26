using CorePortfolio.Domain.Interfaces;
using MediatR;

namespace CorePortfolio.API.Features.MarketIndices.GetMarketIndices;

public sealed record GetMarketIndicesQuery(IReadOnlyList<string> Symbols)
    : IRequest<IReadOnlyList<MarketIndexQuote>>;

public sealed class GetMarketIndicesHandler(IMarketIndexService marketIndexService)
    : IRequestHandler<GetMarketIndicesQuery, IReadOnlyList<MarketIndexQuote>>
{
    public Task<IReadOnlyList<MarketIndexQuote>> Handle(
        GetMarketIndicesQuery request,
        CancellationToken cancellationToken) =>
        marketIndexService.GetQuotesAsync(request.Symbols, cancellationToken);
}
