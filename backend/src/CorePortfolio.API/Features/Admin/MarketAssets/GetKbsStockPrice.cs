using CorePortfolio.Domain.Interfaces;
using MediatR;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record GetKbsStockPriceQuery(string Symbol) : IRequest<decimal?>;

public class GetKbsStockPriceHandler : IRequestHandler<GetKbsStockPriceQuery, decimal?>
{
    private readonly IStockPriceService _stockPriceService;

    public GetKbsStockPriceHandler(IStockPriceService stockPriceService)
    {
        _stockPriceService = stockPriceService;
    }

    public async Task<decimal?> Handle(GetKbsStockPriceQuery request, CancellationToken cancellationToken)
    {
        return await _stockPriceService.GetStockPriceAsync(request.Symbol, cancellationToken);
    }
}
