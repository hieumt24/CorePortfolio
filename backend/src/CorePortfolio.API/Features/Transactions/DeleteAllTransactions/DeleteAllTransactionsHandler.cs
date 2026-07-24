using CorePortfolio.API.Services;
using CorePortfolio.Domain.Accounting;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Transactions.DeleteAllTransactions;

public class DeleteAllTransactionsHandler
    : IRequestHandler<DeleteAllTransactionsCommand, DeleteAllTransactionsResult>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAllTransactionsHandler(
        AppDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteAllTransactionsResult> Handle(
        DeleteAllTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng hiện tại.");

        await using var dbTransaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var transactions = await _dbContext.Transactions
            .Include(transaction => transaction.Asset)
                .ThenInclude(asset => asset!.MarketAsset)
                    .ThenInclude(marketAsset => marketAsset!.Category)
            .Where(transaction =>
                transaction.Portfolio != null &&
                transaction.Portfolio.UserId == userId)
            .ToListAsync(cancellationToken);

        var selectedTransactions = transactions
            .Where(transaction => MatchesGroup(
                transaction.Asset?.MarketAsset?.Category?.Name,
                request.AssetGroup))
            .ToList();

        if (selectedTransactions.Count == 0)
        {
            await dbTransaction.CommitAsync(cancellationToken);
            return new DeleteAllTransactionsResult(0);
        }

        var selectedTransactionIds = selectedTransactions
            .Select(transaction => transaction.Id)
            .ToHashSet();

        var cashLedgerEntries = await _dbContext.CashLedgerEntries
            .Where(entry =>
                entry.Transaction != null &&
                entry.Transaction.Portfolio != null &&
                entry.Transaction.Portfolio.UserId == userId)
            .ToListAsync(cancellationToken);
        cashLedgerEntries = cashLedgerEntries
            .Where(entry =>
                entry.TransactionId.HasValue &&
                selectedTransactionIds.Contains(entry.TransactionId.Value))
            .ToList();

        var linkedCashflows = await _dbContext.CashflowRecords
            .Where(cashflow =>
                cashflow.UserId == userId &&
                cashflow.Transaction != null &&
                cashflow.Transaction.Portfolio != null &&
                cashflow.Transaction.Portfolio.UserId == userId)
            .ToListAsync(cancellationToken);
        linkedCashflows = linkedCashflows
            .Where(cashflow =>
                cashflow.TransactionId.HasValue &&
                selectedTransactionIds.Contains(cashflow.TransactionId.Value))
            .ToList();

        _dbContext.CashLedgerEntries.RemoveRange(cashLedgerEntries);
        _dbContext.CashflowRecords.RemoveRange(linkedCashflows);
        _dbContext.Transactions.RemoveRange(selectedTransactions);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);
        return new DeleteAllTransactionsResult(selectedTransactions.Count);
    }

    private static bool MatchesGroup(string? categoryName, TransactionAssetGroup assetGroup) =>
        assetGroup switch
        {
            TransactionAssetGroup.All => true,
            TransactionAssetGroup.Crypto => AssetCategoryClassifier.IsCrypto(categoryName),
            TransactionAssetGroup.Stock => AssetCategoryClassifier.IsStock(categoryName),
            TransactionAssetGroup.Fund => AssetCategoryClassifier.IsFund(categoryName),
            _ => false
        };
}
