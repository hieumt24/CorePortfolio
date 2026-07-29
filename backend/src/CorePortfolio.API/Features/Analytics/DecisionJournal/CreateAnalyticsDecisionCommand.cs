using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Analytics.GetAnalyticsOverview;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Analytics;
using CorePortfolio.Infrastructure.Data;
using MediatR;

namespace CorePortfolio.API.Features.Analytics.DecisionJournal;

public sealed record CreateAnalyticsDecisionCommand(
    CreateAnalyticsDecisionRequest Request) : IRequest<AnalyticsDecisionDto>;

public sealed class CreateAnalyticsDecisionHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IMediator mediator)
    : IRequestHandler<CreateAnalyticsDecisionCommand, AnalyticsDecisionDto>
{
    public async Task<AnalyticsDecisionDto> Handle(
        CreateAnalyticsDecisionCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ??
            throw new UnauthorizedAccessException();
        var request = command.Request;
        ValidateText(request.Title, 3, 120, "Tiêu đề");
        ValidateText(request.Rationale, 10, 2000, "Luận điểm");
        ValidateText(request.PlannedAction, 3, 1000, "Hành động dự kiến");
        ValidateOptionalText(request.RiskTriggers, 1000, "Điều kiện xem xét lại");
        if (string.IsNullOrWhiteSpace(request.Currency))
            throw new RequestValidationException(
                "Đơn vị tiền tệ là bắt buộc.");
        if (!Enum.TryParse<AnalyticsDecisionType>(
            request.DecisionType,
            true,
            out var decisionType))
        {
            throw new RequestValidationException(
                "Loại quyết định không được hỗ trợ.");
        }

        var reviewDate = AnalyticsDecisionPolicy.NormalizeDate(request.ReviewDate);
        var utcToday = DateTime.UtcNow.Date;
        if (!AnalyticsDecisionPolicy.IsReviewDateAllowed(reviewDate, utcToday))
            throw new RequestValidationException(
                "Ngày xem lại phải từ hôm nay đến tối đa 5 năm tới.");

        var overview = await mediator.Send(
            new GetAnalyticsOverviewQuery(
                request.PortfolioId,
                request.From,
                request.To,
                request.Currency),
            cancellationToken);
        var now = DateTime.UtcNow;
        var decision = new AnalyticsDecision
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PortfolioId = overview.Scope.PortfolioId,
            IsPortfolioScope = overview.Scope.PortfolioId.HasValue,
            PortfolioNameSnapshot = overview.Scope.PortfolioName,
            DecisionType = decisionType,
            Title = request.Title.Trim(),
            Rationale = request.Rationale.Trim(),
            PlannedAction = request.PlannedAction.Trim(),
            RiskTriggers = request.RiskTriggers?.Trim() ?? string.Empty,
            ReviewDate = reviewDate,
            Status = AnalyticsDecisionStatus.Open,
            ScopeFrom = DateTime.SpecifyKind(overview.Scope.From.Date, DateTimeKind.Utc),
            ScopeTo = DateTime.SpecifyKind(overview.Scope.To.Date, DateTimeKind.Utc),
            Currency = overview.Scope.Currency,
            DataQualityStatus = overview.DataQuality.QualityStatus,
            TrackedPortfolioValue = overview.Allocation.Sum(item => item.TotalValue),
            TimeWeightedReturnPercentage =
                overview.Performance.TimeWeightedReturnPercentage.Value,
            MoneyWeightedReturnPercentage =
                overview.Performance.MoneyWeightedReturnPercentage.Value,
            MaximumDrawdownPercentage =
                overview.Performance.MaximumDrawdownPercentage.Value,
            InsightCodes = string.Join(
                ',',
                overview.Insights.Items
                    .Select(item => item.Code)
                    .Distinct(StringComparer.Ordinal)
                    .Take(8)),
            MethodologyVersion = "decision-journal-v1",
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.AnalyticsDecisions.Add(decision);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AnalyticsDecisionMapper.ToDto(decision, utcToday);
    }

    private static void ValidateText(
        string? value,
        int minimumLength,
        int maximumLength,
        string label)
    {
        var length = value?.Trim().Length ?? 0;
        if (length < minimumLength || length > maximumLength)
            throw new RequestValidationException(
                $"{label} phải có từ {minimumLength} đến {maximumLength} ký tự.");
    }

    private static void ValidateOptionalText(
        string? value,
        int maximumLength,
        string label)
    {
        if ((value?.Trim().Length ?? 0) > maximumLength)
            throw new RequestValidationException(
                $"{label} không được vượt quá {maximumLength} ký tự.");
    }
}
