using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.CreateCashflowRecord;

public record CreateCashflowRecordCommand(Guid PortfolioId, Guid CategoryId, decimal Amount, string Currency, DateTime Date, string Description) : IRequest<Guid>;

public class CreateCashflowRecordHandler : IRequestHandler<CreateCashflowRecordCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateCashflowRecordHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateCashflowRecordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId.Value;

        // Verify Portfolio belongs to User
        var portfolio = await _dbContext.Portfolios
            .FirstOrDefaultAsync(p => p.Id == request.PortfolioId && p.UserId == userId, cancellationToken);
            
        if (portfolio == null)
            throw new Exception("Portfolio not found.");

        // Get Category
        var category = await _dbContext.CashflowCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && (c.IsGlobal || c.UserId == userId), cancellationToken);

        if (category == null)
            throw new Exception("Category not found.");

        // Create the Cashflow Record
        var cashflowRecord = new CashflowRecord
        {
            UserId = userId,
            PortfolioId = request.PortfolioId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Currency = request.Currency,
            Date = request.Date,
            Description = request.Description
        };

        // Linked Portfolio Transaction (Deposit/Withdrawal of Fiat Cash)
        // Find if user already has a Fiat Asset in this currency
        var fiatCategory = await _dbContext.AssetCategories.FirstOrDefaultAsync(c => c.Name == "Fiat", cancellationToken);
        var marketAsset = await _dbContext.MarketAssets
            .FirstOrDefaultAsync(ma => ma.CategoryId == fiatCategory.Id && ma.Symbol == request.Currency, cancellationToken);

        if (marketAsset != null)
        {
            var asset = await _dbContext.Assets
                .FirstOrDefaultAsync(a => a.PortfolioId == request.PortfolioId && a.MarketAssetId == marketAsset.Id, cancellationToken);

            if (asset == null)
            {
                asset = new Asset
                {
                    PortfolioId = request.PortfolioId,
                    MarketAssetId = marketAsset.Id
                };
                _dbContext.Assets.Add(asset);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var transaction = new Transaction
            {
                PortfolioId = request.PortfolioId,
                AssetId = asset.Id,
                Type = category.Type == CashflowType.Income ? TransactionType.Deposit : TransactionType.Withdrawal,
                Quantity = request.Amount,
                Price = 1, // Fiat price is 1
                Date = request.Date
            };
            
            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync(cancellationToken);

            cashflowRecord.TransactionId = transaction.Id;
        }

        _dbContext.CashflowRecords.Add(cashflowRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return cashflowRecord.Id;
    }
}
