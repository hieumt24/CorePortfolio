using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Analytics.DecisionJournal;

public sealed record GetAnalyticsDecisionsQuery(
    Guid? PortfolioId,
    string? Status) : IRequest<IReadOnlyList<AnalyticsDecisionDto>>;

public sealed class GetAnalyticsDecisionsHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetAnalyticsDecisionsQuery, IReadOnlyList<AnalyticsDecisionDto>>
{
    public async Task<IReadOnlyList<AnalyticsDecisionDto>> Handle(
        GetAnalyticsDecisionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ??
            throw new UnauthorizedAccessException();
        AnalyticsDecisionStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<AnalyticsDecisionStatus>(
                request.Status,
                true,
                out var parsedStatus))
            {
                throw new RequestValidationException(
                    "Trạng thái nhật ký phải là Open hoặc Reviewed.");
            }
            status = parsedStatus;
        }

        var decisions = dbContext.AnalyticsDecisions
            .AsNoTracking()
            .Where(item => item.UserId == userId);
        if (request.PortfolioId.HasValue)
            decisions = decisions.Where(item => item.PortfolioId == request.PortfolioId);
        if (status.HasValue)
            decisions = decisions.Where(item => item.Status == status.Value);

        var result = await decisions
            .OrderBy(item => item.Status)
            .ThenBy(item => item.ReviewDate)
            .ThenByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var utcToday = DateTime.UtcNow.Date;
        return result
            .Select(item => AnalyticsDecisionMapper.ToDto(item, utcToday))
            .ToList();
    }
}
