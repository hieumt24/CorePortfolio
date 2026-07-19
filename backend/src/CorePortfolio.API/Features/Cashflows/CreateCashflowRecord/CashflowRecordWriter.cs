using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Cashflows.CreateCashflowRecord;

public sealed class CashflowRecordWriter(AppDbContext dbContext, TransactionLedgerService ledgerService)
{
    public async Task<Guid> CreateAsync(
        Guid userId,
        Guid portfolioId,
        Guid categoryId,
        decimal amount,
        string currency,
        DateTime date,
        string description,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
            throw new ArgumentException("Số tiền phải lớn hơn 0.");

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency is not ("VND" or "USD"))
            throw new ArgumentException("Chỉ hỗ trợ tiền tệ VND hoặc USD.");

        var portfolioExists = await dbContext.Portfolios
            .AnyAsync(portfolio => portfolio.Id == portfolioId && portfolio.UserId == userId, cancellationToken);
        if (!portfolioExists)
            throw new ResourceNotFoundException("Không tìm thấy portfolio của người dùng.");

        var category = await dbContext.CashflowCategories
            .SingleOrDefaultAsync(item => item.Id == categoryId && (item.IsGlobal || item.UserId == userId), cancellationToken);
        if (category is null)
            throw new ResourceNotFoundException("Không tìm thấy danh mục thu/chi.");

        await using var databaseTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var fiatCategory = await dbContext.AssetCategories.FirstOrDefaultAsync(item =>
            item.Name == "Fiat" || item.Name == "Tiền pháp định" || item.Name == "Tiền mặt", cancellationToken);
        var marketAsset = await dbContext.MarketAssets.FirstOrDefaultAsync(item =>
            fiatCategory != null && item.CategoryId == fiatCategory.Id && item.Symbol == normalizedCurrency,
            cancellationToken);
        if (marketAsset is null)
            throw new ResourceNotFoundException($"Chưa cấu hình market asset tiền mặt {normalizedCurrency}.");

        var asset = await dbContext.Assets.FirstOrDefaultAsync(item =>
            item.PortfolioId == portfolioId && item.MarketAssetId == marketAsset.Id, cancellationToken);
        if (asset is null)
        {
            asset = new Asset { Id = Guid.NewGuid(), PortfolioId = portfolioId, MarketAssetId = marketAsset.Id };
            dbContext.Assets.Add(asset);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            AssetId = asset.Id,
            Type = category.Type == CashflowType.Income ? TransactionType.Deposit : TransactionType.Withdrawal,
            Quantity = amount,
            Price = 1,
            Notes = description.Trim(),
            Date = date
        };
        var cashflowRecord = new CashflowRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PortfolioId = portfolioId,
            CategoryId = categoryId,
            Amount = amount,
            Currency = normalizedCurrency,
            Date = date,
            Description = description.Trim(),
            TransactionId = transaction.Id
        };

        dbContext.Transactions.Add(transaction);
        await ledgerService.SyncLedgerEntryAsync(transaction, cancellationToken);
        dbContext.CashflowRecords.Add(cashflowRecord);
        await dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        return cashflowRecord.Id;
    }
}
