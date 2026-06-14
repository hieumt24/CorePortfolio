using MediatR;
using CorePortfolio.Domain.Interfaces;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record GetCoinGeckoPriceQuery(string CoinId) : IRequest<decimal?>;

public class GetCoinGeckoPriceHandler : IRequestHandler<GetCoinGeckoPriceQuery, decimal?>
{
    private readonly ICryptoPriceService _cryptoPriceService;

    public GetCoinGeckoPriceHandler(ICryptoPriceService cryptoPriceService)
    {
        _cryptoPriceService = cryptoPriceService;
    }

    public async Task<decimal?> Handle(GetCoinGeckoPriceQuery request, CancellationToken cancellationToken)
    {
        return await _cryptoPriceService.GetPriceAsync(request.CoinId, cancellationToken);
    }
}
