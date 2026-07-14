using CorePortfolio.API.Common;
using CorePortfolio.Domain.Accounting;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Services;

public sealed class TransactionLedgerService
{
    private readonly AppDbContext _dbContext;

    public TransactionLedgerService(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task ValidateHoldingAsync(Transaction candidate, Guid userId, CancellationToken cancellationToken)
    {
        var ownsAsset = await _dbContext.Assets
            .AnyAsync(a => a.Id == candidate.AssetId && a.PortfolioId == candidate.PortfolioId &&
                a.Portfolio!.UserId == userId, cancellationToken);
        if (!ownsAsset) throw new ResourceNotFoundException("Không tìm thấy tài sản trong danh mục.");

        var transactions = await _dbContext.Transactions.AsNoTracking()
            .Where(t => t.AssetId == candidate.AssetId && t.Id != candidate.Id)
            .ToListAsync(cancellationToken);
        transactions.Add(candidate);
        PortfolioAccountingCalculator.Calculate(transactions, 0);
    }

    public async Task ValidateAfterDeleteAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        var remaining = await _dbContext.Transactions.AsNoTracking()
            .Where(t => t.AssetId == transaction.AssetId && t.Id != transaction.Id)
            .ToListAsync(cancellationToken);
        PortfolioAccountingCalculator.Calculate(remaining, 0);
    }

    public async Task SyncLedgerEntryAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        var currency = await _dbContext.Assets
            .Where(a => a.Id == transaction.AssetId)
            .Select(a => a.MarketAsset!.Category!.DefaultCurrency)
            .SingleAsync(cancellationToken);
        currency = currency.Trim().ToUpperInvariant();
        if (currency is not ("VND" or "USD"))
            throw new AccountingValidationException("CorePortfolio hiện chỉ hỗ trợ tài khoản tiền VND và USD.");

        var account = await _dbContext.CashAccounts
            .SingleOrDefaultAsync(a => a.PortfolioId == transaction.PortfolioId && a.Currency == currency, cancellationToken);
        if (account == null)
        {
            account = new CashAccount { Id = Guid.NewGuid(), PortfolioId = transaction.PortfolioId, Currency = currency };
            _dbContext.CashAccounts.Add(account);
        }

        var entry = await _dbContext.CashLedgerEntries
            .SingleOrDefaultAsync(e => e.TransactionId == transaction.Id, cancellationToken);
        if (entry == null)
        {
            entry = new CashLedgerEntry { Id = Guid.NewGuid(), TransactionId = transaction.Id };
            _dbContext.CashLedgerEntries.Add(entry);
        }

        var gross = transaction.Quantity * transaction.Price;
        entry.CashAccount = account;
        entry.CashAccountId = account.Id;
        entry.OccurredAt = transaction.Date;
        entry.Type = transaction.Type switch
        {
            TransactionType.Buy => CashLedgerEntryType.Buy,
            TransactionType.Sell => CashLedgerEntryType.Sell,
            TransactionType.Dividend => CashLedgerEntryType.Dividend,
            TransactionType.Deposit => CashLedgerEntryType.Deposit,
            TransactionType.Withdrawal => CashLedgerEntryType.Withdrawal,
            _ => throw new AccountingValidationException("Loại giao dịch không hợp lệ.")
        };
        entry.Amount = transaction.Type switch
        {
            TransactionType.Buy => -(gross + transaction.Fee),
            TransactionType.Sell or TransactionType.Dividend => gross - transaction.Fee,
            TransactionType.Deposit => gross,
            TransactionType.Withdrawal => -gross,
            _ => 0
        };
        entry.Description = string.IsNullOrWhiteSpace(transaction.Notes)
            ? $"{transaction.Type} transaction"
            : transaction.Notes;
    }
}
