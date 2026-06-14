using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Portfolios.CreatePortfolio;

public class CreatePortfolioHandler : IRequestHandler<CreatePortfolioCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreatePortfolioHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreatePortfolioCommand request, CancellationToken cancellationToken)
    {
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UserId = _currentUserService.UserId ?? Guid.Empty
        };

        _dbContext.Portfolios.Add(portfolio);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return portfolio.Id;
    }
}
