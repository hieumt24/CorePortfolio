using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Analytics.DecisionJournal;

public sealed record ReviewAnalyticsDecisionCommand(
    Guid Id,
    ReviewAnalyticsDecisionRequest Request) : IRequest<AnalyticsDecisionDto>;

public sealed class ReviewAnalyticsDecisionHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<ReviewAnalyticsDecisionCommand, AnalyticsDecisionDto>
{
    public async Task<AnalyticsDecisionDto> Handle(
        ReviewAnalyticsDecisionCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ??
            throw new UnauthorizedAccessException();
        if (!Enum.TryParse<AnalyticsDecisionOutcome>(
            command.Request.Outcome,
            true,
            out var outcome))
        {
            throw new RequestValidationException(
                "Kết quả review phải là OnTrack, Adjust hoặc Closed.");
        }

        var notes = command.Request.Notes?.Trim() ?? string.Empty;
        if (notes.Length is < 3 or > 2000)
            throw new RequestValidationException(
                "Ghi chú review phải có từ 3 đến 2000 ký tự.");

        var decision = await dbContext.AnalyticsDecisions
            .FirstOrDefaultAsync(
                item => item.Id == command.Id && item.UserId == userId,
                cancellationToken)
            ?? throw new ResourceNotFoundException(
                "Không tìm thấy quyết định trong nhật ký.");
        if (decision.Status == AnalyticsDecisionStatus.Reviewed)
            throw new ResourceConflictException(
                "Quyết định này đã được review.");

        var now = DateTime.UtcNow;
        decision.Status = AnalyticsDecisionStatus.Reviewed;
        decision.ReviewOutcome = outcome;
        decision.ReviewNotes = notes;
        decision.ReviewedAt = now;
        decision.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return AnalyticsDecisionMapper.ToDto(decision, now.Date);
    }
}
