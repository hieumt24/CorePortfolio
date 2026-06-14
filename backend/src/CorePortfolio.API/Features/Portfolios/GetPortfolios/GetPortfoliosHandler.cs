using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Portfolios.GetPortfolios;

public class GetPortfoliosHandler : IRequestHandler<GetPortfoliosQuery, List<PortfolioDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetPortfoliosHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<PortfolioDto>> Handle(GetPortfoliosQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Portfolios
            .AsNoTracking()
            .Where(p => p.UserId == _currentUserService.UserId)
            .Select(p => new PortfolioDto(p.Id, p.Name, p.Description, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
