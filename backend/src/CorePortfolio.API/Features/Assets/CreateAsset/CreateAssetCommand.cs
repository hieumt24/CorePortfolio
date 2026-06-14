using CorePortfolio.Domain.Entities;
using MediatR;

namespace CorePortfolio.API.Features.Assets.CreateAsset;

public record CreateAssetCommand(Guid PortfolioId, Guid MarketAssetId) : IRequest<Guid>;
