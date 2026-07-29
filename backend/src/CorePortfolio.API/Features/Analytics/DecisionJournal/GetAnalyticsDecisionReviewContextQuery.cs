using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Analytics.GetAnalyticsOverview;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Analytics;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Analytics.DecisionJournal;

public sealed record GetAnalyticsDecisionReviewContextQuery(
    Guid Id) : IRequest<AnalyticsDecisionReviewContextDto>;

public sealed class GetAnalyticsDecisionReviewContextHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IMediator mediator)
    : IRequestHandler<
        GetAnalyticsDecisionReviewContextQuery,
        AnalyticsDecisionReviewContextDto>
{
    public async Task<AnalyticsDecisionReviewContextDto> Handle(
        GetAnalyticsDecisionReviewContextQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ??
            throw new UnauthorizedAccessException();
        var decision = await dbContext.AnalyticsDecisions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == request.Id && item.UserId == userId,
                cancellationToken)
            ?? throw new ResourceNotFoundException(
                "Không tìm thấy quyết định trong nhật ký.");
        var baseline = AnalyticsDecisionMapper
            .ToDto(decision, DateTime.UtcNow.Date)
            .Snapshot;
        var baselineCodes = baseline.InsightCodes;

        if (decision.IsPortfolioScope && !decision.PortfolioId.HasValue)
        {
            return CreateUnavailable(
                decision.Id,
                baseline,
                baselineCodes,
                "Danh mục gốc đã bị xóa; hệ thống không thay thế bằng dữ liệu của danh mục khác.");
        }

        if (decision.PortfolioId.HasValue)
        {
            var portfolioExists = await dbContext.Portfolios
                .AsNoTracking()
                .AnyAsync(
                    portfolio => portfolio.Id == decision.PortfolioId.Value &&
                        portfolio.UserId == userId,
                    cancellationToken);
            if (!portfolioExists)
            {
                return CreateUnavailable(
                    decision.Id,
                    baseline,
                    baselineCodes,
                    "Danh mục gốc không còn khả dụng trong tài khoản.");
            }
        }

        var periodDays = Math.Clamp(
            (decision.ScopeTo.Date - decision.ScopeFrom.Date).Days,
            0,
            3659);
        var currentTo = DateTime.UtcNow.Date;
        var currentFrom = currentTo.AddDays(-periodDays);
        var overview = await mediator.Send(
            new GetAnalyticsOverviewQuery(
                decision.PortfolioId,
                currentFrom,
                currentTo,
                decision.Currency),
            cancellationToken);
        var current = new AnalyticsDecisionSnapshotDto(
            overview.Scope.From,
            overview.Scope.To,
            overview.Scope.Currency,
            overview.DataQuality.QualityStatus,
            overview.InvestmentPortfolioValue,
            overview.Performance.TimeWeightedReturnPercentage.Value,
            overview.Performance.MoneyWeightedReturnPercentage.Value,
            overview.Performance.MaximumDrawdownPercentage.Value,
            overview.Insights.Items.Select(item => item.Code).ToList(),
            "review-context-v1");
        var comparison = AnalyticsDecisionReviewEngine.Compare(
            new AnalyticsDecisionReviewInput(
                true,
                baseline.DataQualityStatus,
                current.DataQualityStatus,
                baseline.TrackedPortfolioValue,
                current.TrackedPortfolioValue,
                baseline.TimeWeightedReturnPercentage,
                current.TimeWeightedReturnPercentage,
                baseline.MoneyWeightedReturnPercentage,
                current.MoneyWeightedReturnPercentage,
                baseline.MaximumDrawdownPercentage,
                current.MaximumDrawdownPercentage,
                baseline.InsightCodes,
                current.InsightCodes));

        return new AnalyticsDecisionReviewContextDto(
            decision.Id,
            DateTime.UtcNow,
            "review-context-v1",
            comparison.Readiness switch
            {
                AnalyticsDecisionReviewReadiness.Caution =>
                    "Dữ liệu hiện tại chưa hoàn chỉnh; chỉ nên dùng chênh lệch như tín hiệu định hướng.",
                AnalyticsDecisionReviewReadiness.Unavailable =>
                    "Dữ liệu hiện tại chưa đủ để đối chiếu định lượng.",
                _ => null
            },
            baseline,
            current,
            ToDto(comparison),
            "Đối chiếu phản ánh dữ liệu đã ghi nhận trong hai cửa sổ cùng độ dài; không chứng minh quan hệ nhân quả và không phải khuyến nghị đầu tư.");
    }

    private static AnalyticsDecisionReviewContextDto CreateUnavailable(
        Guid decisionId,
        AnalyticsDecisionSnapshotDto baseline,
        IReadOnlyList<string> baselineCodes,
        string reason)
    {
        var comparison = AnalyticsDecisionReviewEngine.Compare(
            new AnalyticsDecisionReviewInput(
                false,
                baseline.DataQualityStatus,
                null,
                baseline.TrackedPortfolioValue,
                null,
                baseline.TimeWeightedReturnPercentage,
                null,
                baseline.MoneyWeightedReturnPercentage,
                null,
                baseline.MaximumDrawdownPercentage,
                null,
                baselineCodes,
                []));
        return new AnalyticsDecisionReviewContextDto(
            decisionId,
            DateTime.UtcNow,
            "review-context-v1",
            reason,
            baseline,
            null,
            ToDto(comparison),
            "Không có dữ liệu hiện tại thay thế; bản ghi gốc vẫn được giữ nguyên để tham khảo.");
    }

    private static AnalyticsDecisionReviewComparisonDto ToDto(
        AnalyticsDecisionReviewComparison comparison) =>
        new(
            comparison.Readiness,
            comparison.Confidence,
            ToDto(comparison.TrackedPortfolioValue),
            comparison.TrackedPortfolioValueChangePercentage,
            ToDto(comparison.TimeWeightedReturnPercentage),
            ToDto(comparison.MoneyWeightedReturnPercentage),
            ToDto(comparison.MaximumDrawdownPercentage),
            comparison.NewInsightCodes,
            comparison.ResolvedInsightCodes,
            comparison.PersistentInsightCodes);

    private static AnalyticsDecisionMetricComparisonDto ToDto(
        AnalyticsDecisionMetricComparison metric) =>
        new(metric.Baseline, metric.Current, metric.Delta);
}
