using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;
using CorePortfolio.API.Common;

namespace CorePortfolio.API.Features.Transactions.CreateTransaction;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, TransactionMutationResult>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly TransactionLedgerService _ledgerService;

    public CreateTransactionHandler(AppDbContext dbContext, ICurrentUserService currentUserService, TransactionLedgerService ledgerService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _ledgerService = ledgerService;
    }

    public async Task<TransactionMutationResult> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var portfolioExists = await _dbContext.Portfolios.AnyAsync(p => p.Id == request.PortfolioId && p.UserId == userId, cancellationToken);
        if (!portfolioExists)
            throw new ResourceNotFoundException("Portfolio not found.");

        if (!request.AssetId.HasValue && !request.MarketAssetId.HasValue)
            throw new RequestValidationException("Asset or Market Asset is required.");

        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var asset = request.AssetId.HasValue
            ? await _dbContext.Assets.FirstOrDefaultAsync(
                a => a.Id == request.AssetId.Value && a.PortfolioId == request.PortfolioId,
                cancellationToken)
            : await _dbContext.Assets.FirstOrDefaultAsync(
                a => a.PortfolioId == request.PortfolioId && a.MarketAssetId == request.MarketAssetId!.Value,
                cancellationToken);

        if (request.AssetId.HasValue && asset == null)
            throw new ResourceNotFoundException("Asset not found in this portfolio.");

        if (asset == null)
        {
            if (!request.MarketAssetId.HasValue)
                throw new RequestValidationException("Market Asset is required when the portfolio asset does not exist.");

            var marketAssetId = request.MarketAssetId.Value;
            var marketAssetExists = await _dbContext.MarketAssets
                .AnyAsync(m => m.Id == marketAssetId, cancellationToken);
            if (!marketAssetExists)
                throw new ResourceNotFoundException("Market Asset not found.");

            asset = new Asset
            {
                Id = Guid.NewGuid(),
                PortfolioId = request.PortfolioId,
                MarketAssetId = marketAssetId
            };
            _dbContext.Assets.Add(asset);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = request.PortfolioId,
            AssetId = asset.Id,
            Type = request.Type,
            Quantity = request.Quantity,
            Price = request.Price,
            Fee = request.Fee,
            Notes = request.Notes?.Trim() ?? string.Empty,
            Date = request.Timestamp ?? DateTime.UtcNow
        };

        await _ledgerService.ValidateHoldingAsync(transaction, userId, cancellationToken);
        _dbContext.Transactions.Add(transaction);
        await _ledgerService.SyncLedgerEntryAsync(transaction, cancellationToken, request.Currency);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        var ledger = await _dbContext.CashLedgerEntries.Include(e => e.CashAccount)
            .SingleAsync(e => e.TransactionId == transaction.Id, cancellationToken);
        return new TransactionMutationResult(transaction.Id, ledger.Amount, ledger.CashAccount.Currency);
    }
}
