using CorePortfolio.Domain.Interfaces;
using MediatR;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record GetDnseStockPriceQuery(string Symbol) : IRequest<decimal?>;

public class GetDnseStockPriceHandler : IRequestHandler<GetDnseStockPriceQuery, decimal?>
{
    private readonly IStockPriceService _stockPriceService;

    public GetDnseStockPriceHandler(IStockPriceService stockPriceService)
    {
        _stockPriceService = stockPriceService;
    }

    public async Task<decimal?> Handle(GetDnseStockPriceQuery request, CancellationToken cancellationToken)
    {
        return await _stockPriceService.GetStockPriceAsync(request.Symbol, cancellationToken);
    }
}
