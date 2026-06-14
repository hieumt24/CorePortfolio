using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Transactions.CreateTransaction;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateTransactionHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var portfolioExists = await _dbContext.Portfolios.AnyAsync(p => p.Id == request.PortfolioId && p.UserId == _currentUserService.UserId, cancellationToken);
        if (!portfolioExists)
            throw new Exception("Portfolio not found");

        var asset = await _dbContext.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId && a.PortfolioId == request.PortfolioId, cancellationToken);
        if (asset == null)
            throw new Exception("Asset not found in this portfolio");

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = request.PortfolioId,
            AssetId = request.AssetId,
            Type = request.Type,
            Quantity = request.Quantity,
            Price = request.Price,
            Date = request.Timestamp ?? DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
