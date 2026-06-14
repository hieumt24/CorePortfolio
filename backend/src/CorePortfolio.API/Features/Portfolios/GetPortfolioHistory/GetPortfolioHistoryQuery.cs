using CorePortfolio.API.Features.Reports.GetGlobalHistory;
using MediatR;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolioHistory;

public record GetPortfolioHistoryQuery(Guid PortfolioId) : IRequest<List<SnapshotDto>>;
