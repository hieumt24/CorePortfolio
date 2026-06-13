using CorePortfolio.Domain.Entities;
using MediatR;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;

public record AssetSummaryDto(Guid AssetId, string Symbol, string Name, AssetType Type, string Currency, decimal CurrentPrice, decimal TotalQuantity, decimal TotalCost, decimal CurrentValue);

public record PortfolioSummaryDto(Guid PortfolioId, string Name, decimal TotalInvested, decimal CurrentTotalValue, List<AssetSummaryDto> Assets);

public record GetPortfolioSummaryQuery(Guid PortfolioId) : IRequest<PortfolioSummaryDto?>;
