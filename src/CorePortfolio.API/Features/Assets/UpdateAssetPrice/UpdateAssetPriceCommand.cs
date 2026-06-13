using MediatR;

namespace CorePortfolio.API.Features.Assets.UpdateAssetPrice;

public record UpdateAssetPriceCommand(Guid AssetId, decimal NewPrice) : IRequest;
