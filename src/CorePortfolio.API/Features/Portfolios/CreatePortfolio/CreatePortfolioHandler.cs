using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;

namespace CorePortfolio.API.Features.Portfolios.CreatePortfolio;

public class CreatePortfolioHandler : IRequestHandler<CreatePortfolioCommand, Guid>
{
    private readonly AppDbContext _dbContext;

    public CreatePortfolioHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreatePortfolioCommand request, CancellationToken cancellationToken)
    {
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Portfolios.Add(portfolio);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return portfolio.Id;
    }
}
