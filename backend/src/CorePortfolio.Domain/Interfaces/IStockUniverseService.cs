using CorePortfolio.Domain.Entities;

namespace CorePortfolio.Domain.Interfaces;

public interface IStockUniverseService
{
    Task<IReadOnlyList<StockInstrument>> GetGroupInstrumentsAsync(
        string group,
        CancellationToken cancellationToken = default);
}
