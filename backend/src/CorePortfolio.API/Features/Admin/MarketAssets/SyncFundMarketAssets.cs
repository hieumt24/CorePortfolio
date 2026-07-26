using System.Globalization;
using System.Text;
using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.API.Features.Admin.ControlPlane;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public sealed record SyncFundMarketAssetsCommand(Guid CategoryId) : IRequest<SyncFundMarketAssetsResult>, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.MarketDataManage;
}

public sealed record SyncFundMarketAssetsResult(
    int ProviderCount,
    int Created,
    int Updated,
    int Unchanged,
    int WithNav);

public sealed class SyncFundMarketAssetsHandler(
    AppDbContext db,
    IFundNavService fundNavService,
    AuditWriter auditWriter)
    : IRequestHandler<SyncFundMarketAssetsCommand, SyncFundMarketAssetsResult>
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    public async Task<SyncFundMarketAssetsResult> Handle(
        SyncFundMarketAssetsCommand request,
        CancellationToken cancellationToken)
    {
        await SyncLock.WaitAsync(cancellationToken);
        try
        {
            var category = await db.AssetCategories
                .SingleOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
                ?? throw new ResourceNotFoundException("Không tìm thấy danh mục Market Asset.");
            if (!IsFundCategory(category.Name))
                throw new RequestValidationException(
                    "Chứng chỉ quỹ chỉ có thể đồng bộ vào danh mục Quỹ/Fund/Chứng chỉ quỹ.");

            var funds = await fundNavService.GetFundsAsync(cancellationToken);
            var externalIds = funds.Select(item => item.ExternalId).ToArray();
            var symbols = funds.Select(item => item.Symbol).ToArray();
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
            var withNav = 0;
            foreach (var fund in funds)
            {
                if (fund.Nav > 0) withNav++;
                if (!byExternalId.TryGetValue(fund.ExternalId, out var asset)
                    && !bySymbol.TryGetValue(fund.Symbol, out asset))
                {
                    db.MarketAssets.Add(new MarketAsset
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = category.Id,
                        Symbol = fund.Symbol,
                        Name = fund.Name,
                        CurrentPrice = fund.Nav,
                        LastUpdated = fund.Nav > 0 ? fund.AsOf : DateTime.UnixEpoch,
                        PriceSource = "Fmarket",
                        ExternalId = fund.ExternalId,
                        PriceStatus = fund.Nav > 0 ? "Fresh" : "Stale",
                        LastPriceError = fund.Nav > 0 ? null : "Fmarket chưa trả về NAV hợp lệ."
                    });
                    created++;
                    continue;
                }

                var changed = asset.Symbol != fund.Symbol
                    || asset.Name != fund.Name
                    || !asset.PriceSource.Equals("Fmarket", StringComparison.OrdinalIgnoreCase)
                    || asset.ExternalId != fund.ExternalId;
                asset.Symbol = fund.Symbol;
                asset.Name = fund.Name;
                asset.PriceSource = "Fmarket";
                asset.ExternalId = fund.ExternalId;
                if (fund.Nav > 0 && (asset.CurrentPrice != fund.Nav
                    || asset.LastUpdated != fund.AsOf
                    || asset.PriceStatus != "Fresh"
                    || asset.LastPriceError is not null))
                {
                    asset.CurrentPrice = fund.Nav;
                    asset.LastUpdated = fund.AsOf;
                    asset.PriceStatus = "Fresh";
                    asset.LastPriceError = null;
                    changed = true;
                }
                if (changed) updated++;
                else unchanged++;
            }

            auditWriter.Add("FundMarketAssetsSynchronized", "MarketAssetUniverse", "Fmarket", new
            {
                ProviderCount = funds.Count,
                Created = created,
                Updated = updated,
                Unchanged = unchanged,
                WithNav = withNav
            });
            await db.SaveChangesAsync(cancellationToken);
            return new(funds.Count, created, updated, unchanged, withNav);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private static bool IsFundCategory(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character == 'đ' ? 'd' : character));
        }
        var name = builder.ToString().Normalize(NormalizationForm.FormC);
        return name.Contains("fund") || name.Contains("quy") || name.Contains("chung chi");
    }
}
