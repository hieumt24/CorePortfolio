using CorePortfolio.Infrastructure.Data;
using MediatR;

namespace CorePortfolio.API.Features.Portfolios.UpdatePortfolio;

public class UpdatePortfolioHandler : IRequestHandler<UpdatePortfolioCommand, bool>
{
    private readonly AppDbContext _context;

    public UpdatePortfolioHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdatePortfolioCommand request, CancellationToken cancellationToken)
    {
        var portfolio = await _context.Portfolios.FindAsync([request.Id], cancellationToken);
        if (portfolio == null)
            return false;

        portfolio.Name = request.Name;
        portfolio.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
