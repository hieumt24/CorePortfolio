using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public class SearchDnseInstrumentsQuery : IRequest<IEnumerable<StockInstrument>>
{
    public string Query { get; set; } = string.Empty;
}

public class SearchDnseInstrumentsHandler : IRequestHandler<SearchDnseInstrumentsQuery, IEnumerable<StockInstrument>>
{
    private readonly IStockInstrumentService _instrumentService;

    public SearchDnseInstrumentsHandler(IStockInstrumentService instrumentService)
    {
        _instrumentService = instrumentService;
    }

    public async Task<IEnumerable<StockInstrument>> Handle(SearchDnseInstrumentsQuery request, CancellationToken cancellationToken)
    {
        return await _instrumentService.SearchInstrumentsAsync(request.Query, limit: 10, cancellationToken);
    }
}
