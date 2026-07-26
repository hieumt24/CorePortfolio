using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Accounting;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public sealed record SyncCryptoMarketAssetsCommand(Guid CategoryId, int? Limit = null)
    : IRequest<SyncCryptoMarketAssetsResult>;

public sealed record SyncCryptoMarketAssetsResult(
    int ProviderCount,
    int Created,
    int Updated,
    int Unchanged,
    int WithPrice);

public sealed class SyncCryptoMarketAssetsHandler(
    AppDbContext db,
    ICryptoMarketService cryptoMarketService,
    AuditWriter auditWriter,
    IConfiguration configuration)
    : IRequestHandler<SyncCryptoMarketAssetsCommand, SyncCryptoMarketAssetsResult>
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    public async Task<SyncCryptoMarketAssetsResult> Handle(
        SyncCryptoMarketAssetsCommand request,
        CancellationToken cancellationToken)
    {
        var limit = request.Limit
            ?? configuration.GetValue("CoinGecko:UniverseSize", 100);
        if (limit is < 1 or > 250)
            throw new RequestValidationException("Số lượng crypto phải từ 1 đến 250.");

        await SyncLock.WaitAsync(cancellationToken);
        try
        {
            var category = await db.AssetCategories
                .SingleOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
                ?? throw new ResourceNotFoundException("Không tìm thấy danh mục Market Asset.");
            if (!AssetCategoryClassifier.IsCrypto(category.Name))
                throw new RequestValidationException(
                    "Crypto chỉ có thể đồng bộ vào danh mục Crypto/Tiền mã hóa/Tiền điện tử.");

            var markets = (await cryptoMarketService.GetTopMarketsAsync(limit, cancellationToken))
                .Where(item => !string.IsNullOrWhiteSpace(item.ExternalId)
                    && !string.IsNullOrWhiteSpace(item.Symbol)
                    && item.Price > 0)
                .GroupBy(item => item.ExternalId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();
            if (markets.Length == 0)
                throw new HttpRequestException("CoinGecko không trả về danh sách crypto hợp lệ.");

            var externalIds = markets.Select(item => item.ExternalId).ToArray();
            var symbols = markets.Select(item => item.Symbol.ToUpperInvariant()).ToArray();
            var existing = await db.MarketAssets
                .Where(item => item.CategoryId == category.Id
                    && ((item.ExternalId != null && externalIds.Contains(item.ExternalId))
                        || symbols.Contains(item.Symbol.ToUpper())))
                .ToListAsync(cancellationToken);
            var byExternalId = existing
                .Where(item => !string.IsNullOrWhiteSpace(item.ExternalId))
                .GroupBy(item => item.ExternalId!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var bySymbol = existing
                .GroupBy(item => item.Symbol, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            var created = 0;
            var updated = 0;
            var unchanged = 0;
            foreach (var market in markets)
            {
                var symbol = market.Symbol.ToUpperInvariant();
                if (!byExternalId.TryGetValue(market.ExternalId, out var asset)
                    && !bySymbol.TryGetValue(symbol, out asset))
                {
                    asset = new MarketAsset
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = category.Id,
                        Symbol = symbol,
                        Name = market.Name,
                        CurrentPrice = market.Price,
                        LastUpdated = market.AsOf,
                        PriceSource = "CoinGecko",
                        ExternalId = market.ExternalId,
                        PriceStatus = "Fresh"
                    };
                    db.MarketAssets.Add(asset);
                    byExternalId[market.ExternalId] = asset;
                    if (!bySymbol.ContainsKey(symbol))
                        bySymbol[symbol] = asset;
                    created++;
                    continue;
                }

                var changed = !string.Equals(asset.Symbol, symbol, StringComparison.Ordinal)
                    || !string.Equals(asset.Name, market.Name, StringComparison.Ordinal)
                    || !asset.PriceSource.Equals("CoinGecko", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(asset.ExternalId, market.ExternalId, StringComparison.OrdinalIgnoreCase)
                    || asset.CurrentPrice != market.Price
                    || asset.LastUpdated != market.AsOf
                    || !asset.PriceStatus.Equals("Fresh", StringComparison.OrdinalIgnoreCase)
                    || asset.LastPriceError is not null;

                asset.Symbol = symbol;
                asset.Name = market.Name;
                asset.PriceSource = "CoinGecko";
                asset.ExternalId = market.ExternalId;
                asset.CurrentPrice = market.Price;
                asset.LastUpdated = market.AsOf;
                asset.PriceStatus = "Fresh";
                asset.LastPriceError = null;

                if (changed) updated++;
                else unchanged++;
            }

            auditWriter.Add("CryptoMarketAssetsSynchronized", "MarketAssetUniverse", "CoinGecko", new
            {
                RequestedLimit = limit,
                ProviderCount = markets.Length,
                Created = created,
                Updated = updated,
                Unchanged = unchanged,
                WithPrice = markets.Length
            });
            await db.SaveChangesAsync(cancellationToken);
            return new(markets.Length, created, updated, unchanged, markets.Length);
        }
        finally
        {
            SyncLock.Release();
        }
    }
}
