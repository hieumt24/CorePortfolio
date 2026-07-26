using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Interfaces;
using MediatR;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public class SearchKbsInstrumentsQuery : IRequest<IEnumerable<StockInstrument>>
{
    public string Query { get; set; } = string.Empty;
}

public class SearchKbsInstrumentsHandler : IRequestHandler<SearchKbsInstrumentsQuery, IEnumerable<StockInstrument>>
{
    private readonly IStockInstrumentService _instrumentService;

    public SearchKbsInstrumentsHandler(IStockInstrumentService instrumentService)
    {
        _instrumentService = instrumentService;
    }

    public async Task<IEnumerable<StockInstrument>> Handle(SearchKbsInstrumentsQuery request, CancellationToken cancellationToken)
    {
        return await _instrumentService.SearchInstrumentsAsync(request.Query, limit: 10, cancellationToken);
    }
}
