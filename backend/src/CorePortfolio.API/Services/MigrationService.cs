using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Common;

namespace CorePortfolio.API.Services;

public class MigrationService
{
    private readonly AppDbContext _dbContext;
    private readonly TransactionLedgerService _ledgerService;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(AppDbContext dbContext, TransactionLedgerService ledgerService, ILogger<MigrationService> logger)
    {
        _dbContext = dbContext;
        _ledgerService = ledgerService;
        _logger = logger;
    }

    public async Task MigrateLegacyTransactionsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting legacy data migration...");

        if (await _dbContext.CashLedgerEntries.AnyAsync(cancellationToken))
            throw new ResourceConflictException("Ledger đã có dữ liệu. Migration legacy chỉ được phép chạy một lần.");

        await using var databaseTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Fetch all transactions ordered by Date
        var transactions = await _dbContext.Transactions
            .Include(t => t.Asset)
            .ThenInclude(a => a!.MarketAsset)
            .ThenInclude(m => m!.Category)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} transactions to migrate", transactions.Count);

        // Create missing CashAccounts and add them to context so they are tracked
        var currencyGroups = transactions
            .Where(t => t.Asset?.MarketAsset?.Category != null)
            .GroupBy(t => new { t.PortfolioId, Currency = t.Asset!.MarketAsset!.Category!.DefaultCurrency.Trim().ToUpperInvariant() })
            .ToList();

        foreach (var group in currencyGroups)
        {
            if (group.Key.Currency is not ("VND" or "USD")) continue;

            var account = new CashAccount
            {
                Id = Guid.NewGuid(),
                PortfolioId = group.Key.PortfolioId,
                Currency = group.Key.Currency
            };
            _dbContext.CashAccounts.Add(account);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 3. Process each transaction using TransactionLedgerService
        foreach (var transaction in transactions)
        {
            if (transaction.Asset?.MarketAsset?.Category != null)
            {
                await _ledgerService.SyncLedgerEntryAsync(transaction, cancellationToken, allowNegativeBalance: true);
            }
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Fix negative balances with MigratedOpeningBalance
        var accounts = await _dbContext.CashAccounts
            .Include(a => a.Entries)
            .ToListAsync(cancellationToken);

        foreach (var account in accounts)
        {
            var currentBalance = account.Entries.Sum(e => e.Amount);
            if (currentBalance < 0)
            {
                var adjustmentAmount = Math.Abs(currentBalance);
                var entry = new CashLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    CashAccountId = account.Id,
                    Amount = adjustmentAmount,
                    Type = CashLedgerEntryType.MigratedOpeningBalance,
                    Description = "Điều chỉnh số dư âm từ dữ liệu cũ",
                    OccurredAt = transactions.FirstOrDefault()?.Date.AddDays(-1) ?? DateTime.UtcNow
                };
                _dbContext.CashLedgerEntries.Add(entry);
                _logger.LogInformation("Added MigratedOpeningBalance of {Amount} to Account {AccountId}", adjustmentAmount, account.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Legacy data migration completed.");
    }
}
