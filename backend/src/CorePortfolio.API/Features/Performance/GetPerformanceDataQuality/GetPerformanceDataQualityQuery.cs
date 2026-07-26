using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Performance;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Performance.GetPerformanceDataQuality;

public sealed record GetPerformanceDataQualityQuery(
    Guid? PortfolioId,
    DateTime? From,
    DateTime? To) : IRequest<PerformanceDataQualityDto>;

public sealed class GetPerformanceDataQualityHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUser)
    : IRequestHandler<GetPerformanceDataQualityQuery, PerformanceDataQualityDto>
{
    private const int MaximumRangeDays = 3660;
    private const int MaximumMissingDatesReturned = 31;

    public async Task<PerformanceDataQualityDto> Handle(
        GetPerformanceDataQualityQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var to = (request.To ?? DateTime.UtcNow).Date;
        var from = (request.From ?? to.AddDays(-29)).Date;
        var rangeDays = (to - from).Days + 1;

        if (from > to)
            throw new RequestValidationException("Ngày bắt đầu không được sau ngày kết thúc.");
        if (rangeDays > MaximumRangeDays)
            throw new RequestValidationException("Khoảng kiểm tra chất lượng dữ liệu không được vượt quá 10 năm.");

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
        var snapshots = portfolioIds.Count == 0
            ? []
            : await dbContext.PortfolioSnapshots
                .AsNoTracking()
                .Where(snapshot =>
                    portfolioIds.Contains(snapshot.PortfolioId) &&
                    snapshot.Date >= from &&
                    snapshot.Date < to.AddDays(1))
                .Select(snapshot => new SnapshotCoverage(
                    snapshot.PortfolioId,
                    snapshot.Date,
                    snapshot.ValuationTimestamp,
                    snapshot.QualityStatus,
                    snapshot.StaleAssetCount))
                .ToListAsync(cancellationToken);

        var snapshotKeys = snapshots
            .Select(snapshot => (snapshot.PortfolioId, Date: snapshot.Date.Date))
            .ToHashSet();
        var expectedDates = new HashSet<DateTime>();
        var missingDates = new HashSet<DateTime>();
        var expectedSnapshotCount = 0;

        foreach (var portfolio in portfolios)
        {
            var coverageStart = portfolio.CreatedAt.Date > from
                ? portfolio.CreatedAt.Date
                : from;

            for (var date = coverageStart; date <= to; date = date.AddDays(1))
            {
                expectedDates.Add(date);
                expectedSnapshotCount++;
                if (!snapshotKeys.Contains((portfolio.Id, date)))
                    missingDates.Add(date);
            }
        }

        var unknownCashFlowCount = portfolioIds.Count == 0
            ? 0
            : await dbContext.CashLedgerEntries
                .AsNoTracking()
                .CountAsync(entry =>
                    portfolioIds.Contains(entry.CashAccount.PortfolioId) &&
                    entry.OccurredAt >= from &&
                    entry.OccurredAt < to.AddDays(1) &&
                    entry.Classification == CashLedgerEntryClassification.Unknown,
                    cancellationToken);

        var latestSnapshots = snapshots
            .GroupBy(snapshot => snapshot.PortfolioId)
            .Select(group => group
                .OrderByDescending(snapshot => snapshot.Date)
                .ThenByDescending(snapshot => snapshot.ValuationTimestamp)
                .First())
            .ToList();
        var staleAssetCount = latestSnapshots.Sum(snapshot => snapshot.StaleAssetCount);
        var missingSnapshotCount = Math.Max(0, expectedSnapshotCount - snapshots.Count);

        var issues = new List<string>();
        if (portfolios.Count == 0)
            issues.Add("NoPortfolios");
        if (snapshots.Count == 0 && portfolios.Count > 0)
            issues.Add("NoSnapshots");
        if (missingSnapshotCount > 0)
            issues.Add("MissingSnapshots");
        if (snapshots.Any(snapshot =>
                snapshot.StaleAssetCount > 0 ||
                snapshot.QualityStatus == PortfolioSnapshotQuality.StalePrices))
            issues.Add("StalePrices");
        if (unknownCashFlowCount > 0)
            issues.Add("UnclassifiedCashFlows");
        if (snapshots.Any(snapshot =>
                snapshot.QualityStatus == PortfolioSnapshotQuality.Legacy))
            issues.Add("LegacySnapshots");

        var qualityStatus = DetermineQualityStatus(snapshots.Count, issues);
        var missingDateValues = missingDates
            .OrderBy(date => date)
            .Take(MaximumMissingDatesReturned)
            .Select(date => date.ToString("yyyy-MM-dd"))
            .ToList();

        return new PerformanceDataQualityDto(
            from,
            to,
            snapshots.Count == 0
                ? null
                : snapshots.Max(snapshot => snapshot.ValuationTimestamp),
            qualityStatus,
            portfolios.Count,
            snapshots.Count,
            expectedSnapshotCount,
            missingSnapshotCount,
            missingDates.Count,
            missingDateValues,
            staleAssetCount,
            unknownCashFlowCount,
            issues);
    }

    private static string DetermineQualityStatus(
        int snapshotCount,
        IReadOnlyCollection<string> issues)
    {
        if (snapshotCount == 0)
            return PortfolioSnapshotQuality.Unavailable;
        if (issues.Contains("MissingSnapshots") ||
            issues.Contains("UnclassifiedCashFlows") ||
            issues.Contains("LegacySnapshots"))
            return PortfolioSnapshotQuality.Partial;
        if (issues.Contains("StalePrices"))
            return PortfolioSnapshotQuality.StalePrices;
        return PortfolioSnapshotQuality.Complete;
    }

    private sealed record PortfolioCoverage(Guid Id, DateTime CreatedAt);

    private sealed record SnapshotCoverage(
        Guid PortfolioId,
        DateTime Date,
        DateTime ValuationTimestamp,
        string QualityStatus,
        int StaleAssetCount);
}
