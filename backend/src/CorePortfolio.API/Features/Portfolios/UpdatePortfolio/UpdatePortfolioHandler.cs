using CorePortfolio.Infrastructure.Data;
using CorePortfolio.API.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Portfolios.UpdatePortfolio;

public class UpdatePortfolioHandler : IRequestHandler<UpdatePortfolioCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdatePortfolioHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(UpdatePortfolioCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var portfolio = await _context.Portfolios.SingleOrDefaultAsync(
            item => item.Id == request.Id && item.UserId == userId,
            cancellationToken);
        if (portfolio == null)
            return false;

        portfolio.Name = request.Name;
        portfolio.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
