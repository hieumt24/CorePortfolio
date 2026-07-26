using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Performance;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Performance;

public sealed record PerformanceRequest(
    Guid? PortfolioId,
    string AssetGroup,
    DateTime? From,
    DateTime? To,
    string Currency);

public sealed record PerformanceSnapshotData(
    DateTime Date,
    decimal NetAssetValue,
    decimal NetExternalFlow,
    decimal RealizedPnl,
    decimal UnrealizedPnl,
    decimal Income,
    decimal Fees,
    string QualityStatus,
    int StaleAssetCount,
    int UnclassifiedCashFlowCount);

public sealed record PerformanceDataSet(
    string Currency,
    DateTime From,
    DateTime To,
    IReadOnlyList<PerformanceSnapshotData> Snapshots,
    PerformanceQualityDto Quality)
{
    public IReadOnlyList<PerformancePoint> ToPerformancePoints() => Snapshots
        .Select(snapshot => new PerformancePoint(
            snapshot.Date,
            snapshot.NetAssetValue,
            snapshot.NetExternalFlow,
            snapshot.QualityStatus))
        .ToList();
}

public sealed class PerformanceDataService(
    AppDbContext dbContext,
    ICurrentUserService currentUser)
{
    private const int MaximumRangeDays = 3660;

    public async Task<PerformanceDataSet> LoadAsync(
        PerformanceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var assetGroup = string.IsNullOrWhiteSpace(request.AssetGroup)
            ? "All"
            : request.AssetGroup.Trim();
        if (!assetGroup.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(
                "Sprint 4 chỉ hỗ trợ assetGroup=All; dữ liệu snapshot theo nhóm tài sản sẽ được bổ sung cùng benchmark.");
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? "VND"
            : request.Currency.Trim().ToUpperInvariant();
        if (currency is not ("VND" or "USD"))
            throw new RequestValidationException("Currency phải là VND hoặc USD.");

        var to = (request.To ?? DateTime.UtcNow).Date;
        var from = (request.From ?? to.AddYears(-1)).Date;
        var rangeDays = (to - from).Days + 1;
        if (from > to)
            throw new RequestValidationException("Ngày bắt đầu không được sau ngày kết thúc.");
        if (rangeDays > MaximumRangeDays)
            throw new RequestValidationException("Khoảng hiệu suất không được vượt quá 10 năm.");

        var portfolioQuery = dbContext.Portfolios
            .AsNoTracking()
            .Where(portfolio => portfolio.UserId == userId);
        if (request.PortfolioId.HasValue)
            portfolioQuery = portfolioQuery.Where(portfolio => portfolio.Id == request.PortfolioId.Value);

        var portfolios = await portfolioQuery
            .Select(portfolio => new PortfolioCoverage(portfolio.Id, portfolio.CreatedAt))
            .ToListAsync(cancellationToken);
        if (request.PortfolioId.HasValue && portfolios.Count == 0)
            throw new ResourceNotFoundException("Không tìm thấy portfolio của người dùng.");

        var portfolioIds = portfolios.Select(portfolio => portfolio.Id).ToList();
        var rows = portfolioIds.Count == 0
            ? []
            : await dbContext.PortfolioSnapshots
                .AsNoTracking()
                .Where(snapshot =>
                    portfolioIds.Contains(snapshot.PortfolioId) &&
                    snapshot.Date >= from &&
                    snapshot.Date < to.AddDays(1))
                .Select(snapshot => new SnapshotRow(
                    snapshot.PortfolioId,
                    snapshot.Date,
                    snapshot.NetAssetValue,
                    snapshot.NetExternalFlow,
                    snapshot.RealizedPnl,
                    snapshot.UnrealizedPnl,
                    snapshot.Income,
                    snapshot.Fees,
                    snapshot.BaseCurrency,
                    snapshot.UsdToVndRate,
                    snapshot.ValuationTimestamp,
                    snapshot.QualityStatus,
                    snapshot.StaleAssetCount,
                    snapshot.UnclassifiedCashFlowCount))
                .ToListAsync(cancellationToken);

        var snapshots = rows
            .GroupBy(row => row.Date.Date)
            .OrderBy(group => group.Key)
            .Select(group => new PerformanceSnapshotData(
                group.Key,
                group.Sum(row => ConvertCurrency(
                    row.NetAssetValue,
                    row.BaseCurrency,
                    currency,
                    row.UsdToVndRate)),
                group.Sum(row => ConvertCurrency(
                    row.NetExternalFlow,
                    row.BaseCurrency,
                    currency,
                    row.UsdToVndRate)),
                group.Sum(row => ConvertCurrency(
                    row.RealizedPnl,
                    row.BaseCurrency,
                    currency,
                    row.UsdToVndRate)),
                group.Sum(row => ConvertCurrency(
                    row.UnrealizedPnl,
                    row.BaseCurrency,
                    currency,
                    row.UsdToVndRate)),
                group.Sum(row => ConvertCurrency(
                    row.Income,
                    row.BaseCurrency,
                    currency,
                    row.UsdToVndRate)),
                group.Sum(row => ConvertCurrency(
                    row.Fees,
                    row.BaseCurrency,
                    currency,
                    row.UsdToVndRate)),
                AggregateQuality(group.Select(row => row.QualityStatus)),
                group.Sum(row => row.StaleAssetCount),
                group.Sum(row => row.UnclassifiedCashFlowCount)))
            .ToList();

        var rowKeys = rows
            .Select(row => (row.PortfolioId, Date: row.Date.Date))
            .ToHashSet();
        var missingDates = new HashSet<DateTime>();
        foreach (var portfolio in portfolios)
        {
            var coverageStart = portfolio.CreatedAt.Date > from
                ? portfolio.CreatedAt.Date
                : from;
            for (var date = coverageStart; date <= to; date = date.AddDays(1))
            {
                if (!rowKeys.Contains((portfolio.Id, date)))
                    missingDates.Add(date);
            }
        }

        var latestRows = rows
            .GroupBy(row => row.PortfolioId)
            .Select(group => group
                .OrderByDescending(row => row.Date)
                .ThenByDescending(row => row.ValuationTimestamp)
                .First())
            .ToList();
        var staleAssetCount = latestRows.Sum(row => row.StaleAssetCount);
        var unclassifiedCount = rows.Sum(row => row.UnclassifiedCashFlowCount);
        var qualityStatus = DetermineQuality(
            snapshots.Count,
            missingDates.Count,
            rows,
            staleAssetCount,
            unclassifiedCount);

        return new PerformanceDataSet(
            currency,
            from,
            to,
            snapshots,
            new PerformanceQualityDto(
                rows.Count == 0 ? null : rows.Max(row => row.ValuationTimestamp),
                qualityStatus,
                missingDates.Count,
                staleAssetCount,
                unclassifiedCount));
    }

    private static decimal ConvertCurrency(
        decimal amount,
        string sourceCurrency,
        string targetCurrency,
        decimal usdToVndRate)
    {
        if (sourceCurrency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;
        if (usdToVndRate <= 0)
            return amount;
        return targetCurrency == "USD"
            ? amount / usdToVndRate
            : amount * usdToVndRate;
    }

    private static string AggregateQuality(IEnumerable<string> statuses)
    {
        var values = statuses.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (values.Contains(PortfolioSnapshotQuality.Partial))
            return PortfolioSnapshotQuality.Partial;
        if (values.Contains(PortfolioSnapshotQuality.Legacy))
            return PortfolioSnapshotQuality.Legacy;
        if (values.Contains(PortfolioSnapshotQuality.StalePrices))
            return PortfolioSnapshotQuality.StalePrices;
        return PortfolioSnapshotQuality.Complete;
    }

    private static string DetermineQuality(
        int snapshotCount,
        int missingDays,
        IReadOnlyCollection<SnapshotRow> rows,
        int staleAssetCount,
        int unclassifiedCount)
    {
        if (snapshotCount == 0)
            return PortfolioSnapshotQuality.Unavailable;
        if (missingDays > 0 ||
            unclassifiedCount > 0 ||
            rows.Any(row =>
                row.QualityStatus is PortfolioSnapshotQuality.Legacy
                    or PortfolioSnapshotQuality.Partial))
            return PortfolioSnapshotQuality.Partial;
        if (staleAssetCount > 0)
            return PortfolioSnapshotQuality.StalePrices;
        return PortfolioSnapshotQuality.Complete;
    }

    private sealed record PortfolioCoverage(Guid Id, DateTime CreatedAt);

    private sealed record SnapshotRow(
        Guid PortfolioId,
        DateTime Date,
        decimal NetAssetValue,
        decimal NetExternalFlow,
        decimal RealizedPnl,
        decimal UnrealizedPnl,
        decimal Income,
        decimal Fees,
        string BaseCurrency,
        decimal UsdToVndRate,
        DateTime ValuationTimestamp,
        string QualityStatus,
        int StaleAssetCount,
        int UnclassifiedCashFlowCount);
}
