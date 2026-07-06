using CorePortfolio.Domain.Entities;
using MediatR;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;

public record AssetSummaryDto(Guid AssetId, Guid MarketAssetId, string Symbol, string Name, string CategoryName,
    string Currency, decimal CurrentPrice, decimal TotalQuantity, decimal TotalCost, decimal CurrentValue,
    decimal TotalBought, decimal AverageCost, decimal RealizedPnl, decimal UnrealizedPnl, decimal Fees,
    DateTime PriceUpdatedAt);

public record CashBalanceDto(Guid CashAccountId, string Currency, decimal Balance);

public record PortfolioSummaryDto(Guid PortfolioId, string Name, decimal TotalInvested, decimal CurrentTotalValue,
    List<AssetSummaryDto> Assets, List<CashBalanceDto> CashBalances, decimal RealizedPnl,
    decimal UnrealizedPnl, decimal Fees, string BaseCurrency, DateTime AsOf);

public record GetPortfolioSummaryQuery(Guid PortfolioId, Guid? UserId = null) : IRequest<PortfolioSummaryDto?>;
