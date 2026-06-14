using MediatR;

namespace CorePortfolio.API.Features.MarketAssets.UpdateMarketAssetPrice;

public record UpdateMarketAssetPriceCommand(Guid MarketAssetId, decimal NewPrice) : IRequest;
