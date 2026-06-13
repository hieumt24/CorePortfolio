using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Transactions.CreateTransaction;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, Guid>
{
    private readonly AppDbContext _dbContext;

    public CreateTransactionHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var portfolioExists = await _dbContext.Portfolios.AnyAsync(p => p.Id == request.PortfolioId, cancellationToken);
        if (!portfolioExists)
            throw new Exception("Portfolio not found");

        var asset = await _dbContext.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId && a.PortfolioId == request.PortfolioId, cancellationToken);
        if (asset == null)
            throw new Exception("Asset not found in this portfolio");

        if (!string.IsNullOrEmpty(request.Currency) && asset.Currency != request.Currency)
        {
            asset.Currency = request.Currency;
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = request.PortfolioId,
            AssetId = request.AssetId,
            Type = request.Type,
            Quantity = request.Quantity,
            Price = request.Price,
            Date = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
