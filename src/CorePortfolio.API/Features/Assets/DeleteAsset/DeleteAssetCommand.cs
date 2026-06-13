using MediatR;

namespace CorePortfolio.API.Features.Assets.DeleteAsset;

public record DeleteAssetCommand(Guid PortfolioId, Guid AssetId) : IRequest<bool>;
