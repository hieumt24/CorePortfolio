using System.Globalization;
using System.Text;
using CorePortfolio.API.Common;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record SyncVn100MarketAssetsCommand(Guid CategoryId) : IRequest<SyncVn100MarketAssetsResult>, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.MarketDataManage;
}

public record SyncVn100MarketAssetsResult(
    int ProviderCount,
    int Created,
    int Updated,
    int Unchanged,
    int WithReferencePrice);

public sealed class SyncVn100MarketAssetsHandler
    : IRequestHandler<SyncVn100MarketAssetsCommand, SyncVn100MarketAssetsResult>
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);
    private readonly AppDbContext _db;
    private readonly IStockUniverseService _stockUniverse;
    private readonly AuditWriter _auditWriter;

    public SyncVn100MarketAssetsHandler(
        AppDbContext db,
        IStockUniverseService stockUniverse,
        AuditWriter auditWriter)
    {
        _db = db;
        _stockUniverse = stockUniverse;
        _auditWriter = auditWriter;
    }

    public async Task<SyncVn100MarketAssetsResult> Handle(
        SyncVn100MarketAssetsCommand request,
        CancellationToken cancellationToken)
    {
        await SyncLock.WaitAsync(cancellationToken);
        try
        {
        var category = await _db.AssetCategories
            .SingleOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy danh mục Market Asset.");
        if (!IsStockCategory(category.Name))
            throw new RequestValidationException(
                "VN100 chỉ có thể đồng bộ vào danh mục Cổ phiếu/Chứng khoán.");

        var instruments = await _stockUniverse.GetGroupInstrumentsAsync("VN100", cancellationToken);
        if (instruments.Count == 0)
            throw new HttpRequestException("KBS không trả về danh sách VN100.");

        var normalizedInstruments = instruments
            .Where(item => !string.IsNullOrWhiteSpace(item.Symbol))
            .GroupBy(item => item.Symbol.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        var symbols = normalizedInstruments
            .Select(item => item.Symbol.Trim().ToUpperInvariant())
            .ToArray();
        var existingAssets = await _db.MarketAssets
            .Where(item => item.CategoryId == category.Id && symbols.Contains(item.Symbol.ToUpper()))
            .ToListAsync(cancellationToken);
        var existingBySymbol = existingAssets
            .GroupBy(item => item.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var created = 0;
        var updated = 0;
        var unchanged = 0;
        var withReferencePrice = 0;
        var now = DateTime.UtcNow;

        foreach (var instrument in normalizedInstruments)
        {
            var symbol = instrument.Symbol.Trim().ToUpperInvariant();
            var name = string.IsNullOrWhiteSpace(instrument.Name) ? symbol : instrument.Name.Trim();
            var referencePrice = instrument.ReferencePrice is > 0 ? instrument.ReferencePrice.Value : 0m;
            if (referencePrice > 0)
                withReferencePrice++;

            if (!existingBySymbol.TryGetValue(symbol, out var asset))
            {
                _db.MarketAssets.Add(new MarketAsset
                {
                    Id = Guid.NewGuid(),
                    CategoryId = category.Id,
                    Symbol = symbol,
                    Name = name,
                    CurrentPrice = referencePrice,
                    LastUpdated = referencePrice > 0 ? now : DateTime.UnixEpoch,
                    PriceSource = "KBS",
                    ExternalId = symbol,
                    PriceStatus = referencePrice > 0 ? "Fresh" : "Stale",
                    LastPriceError = referencePrice > 0 ? null : "KBS chưa trả về giá tham chiếu."
                });
                created++;
                continue;
            }

            var changed = false;
            if (!string.Equals(asset.Symbol, symbol, StringComparison.Ordinal))
            {
                asset.Symbol = symbol;
                changed = true;
            }
            if (!string.Equals(asset.Name, name, StringComparison.Ordinal))
            {
                asset.Name = name;
                changed = true;
            }
            if (!asset.PriceSource.Equals("KBS", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(asset.ExternalId, symbol, StringComparison.Ordinal))
            {
                asset.PriceSource = "KBS";
                asset.ExternalId = symbol;
                asset.PriceStatus = "Stale";
                asset.LastPriceError = null;
                changed = true;
            }
            if (referencePrice > 0
                && (asset.CurrentPrice != referencePrice
                    || !asset.PriceStatus.Equals("Fresh", StringComparison.OrdinalIgnoreCase)
                    || asset.LastPriceError is not null
                    || asset.LastUpdated < now.AddMinutes(-5)))
            {
                asset.CurrentPrice = referencePrice;
                asset.LastUpdated = now;
                asset.PriceStatus = "Fresh";
                asset.LastPriceError = null;
                changed = true;
            }

            if (changed) updated++;
            else unchanged++;
        }

        _auditWriter.Add(
            "Vn100MarketAssetsSynchronized",
            "MarketAssetUniverse",
            "VN100",
            new
            {
                ProviderCount = normalizedInstruments.Length,
                Created = created,
                Updated = updated,
                Unchanged = unchanged,
                WithReferencePrice = withReferencePrice
            });
        await _db.SaveChangesAsync(cancellationToken);
        return new SyncVn100MarketAssetsResult(
            normalizedInstruments.Length,
            created,
            updated,
            unchanged,
            withReferencePrice);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private static bool IsStockCategory(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character == 'đ' ? 'd' : character));
        }

        var name = builder.ToString().Normalize(NormalizationForm.FormC);
        return name.Contains("stock")
            || name.Contains("co phieu")
            || name.Contains("chung khoan");
    }
}
