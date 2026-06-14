using MediatR;

namespace CorePortfolio.API.Features.Portfolios.UpdatePortfolio;

public record UpdatePortfolioCommand(Guid Id, string Name, string Description) : IRequest<bool>;
