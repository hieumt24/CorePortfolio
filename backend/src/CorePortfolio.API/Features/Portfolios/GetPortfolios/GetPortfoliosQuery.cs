using MediatR;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolios;

public record PortfolioDto(Guid Id, string Name, string Description, DateTime CreatedAt);

public record GetPortfoliosQuery() : IRequest<List<PortfolioDto>>;
