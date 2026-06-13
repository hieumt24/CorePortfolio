using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolios;

public class GetPortfoliosHandler : IRequestHandler<GetPortfoliosQuery, List<PortfolioDto>>
{
    private readonly AppDbContext _dbContext;

    public GetPortfoliosHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<PortfolioDto>> Handle(GetPortfoliosQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Portfolios
            .AsNoTracking()
            .Select(p => new PortfolioDto(p.Id, p.Name, p.Description, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
