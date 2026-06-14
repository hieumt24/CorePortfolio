using CorePortfolio.Domain.Entities;

namespace CorePortfolio.Domain.Interfaces;

public interface IStockInstrumentService
{
    Task<IEnumerable<StockInstrument>> SearchInstrumentsAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
}
