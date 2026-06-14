using MediatR;

namespace CorePortfolio.API.Features.Portfolios.CreatePortfolio;

public record CreatePortfolioCommand(string Name, string Description) : IRequest<Guid>;
