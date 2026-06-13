using CorePortfolio.Domain.Entities;
using MediatR;

namespace CorePortfolio.API.Features.Assets.CreateAsset;

public record CreateAssetCommand(Guid PortfolioId, string Symbol, string Name, AssetType Type, string Currency) : IRequest<Guid>;
