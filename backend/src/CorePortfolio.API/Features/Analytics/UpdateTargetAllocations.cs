using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Analytics;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Analytics;

public class TargetAllocationInput
{
    public Guid CategoryId { get; set; }
    public decimal TargetPercentage { get; set; }
}

public sealed record UpdateTargetAllocationsCommand(
    List<TargetAllocationInput> Allocations) : IRequest<bool>;

public class UpdateTargetAllocationsHandler : IRequestHandler<UpdateTargetAllocationsCommand, bool>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTargetAllocationsHandler(
        AppDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(UpdateTargetAllocationsCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var assessment = TargetAllocationPolicy.Evaluate(
            request.Allocations.Select(allocation =>
                new TargetAllocationWeight(
                    allocation.CategoryId,
                    allocation.TargetPercentage)));
        if (assessment.Status == TargetAllocationPlanStatuses.Invalid)
            throw new RequestValidationException(
                assessment.Reason ?? "Kế hoạch phân bổ mục tiêu không hợp lệ.");

        var categoryIds = request.Allocations
            .Select(allocation => allocation.CategoryId)
            .Distinct()
            .ToList();
        var existingCategoryCount = categoryIds.Count == 0
            ? 0
            : await _dbContext.AssetCategories
                .CountAsync(category => categoryIds.Contains(category.Id), cancellationToken);
        if (existingCategoryCount != categoryIds.Count)
            throw new RequestValidationException(
                "Kế hoạch phân bổ chứa nhóm tài sản không tồn tại.");

        var existing = await _dbContext.TargetAllocations
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);
        _dbContext.TargetAllocations.RemoveRange(existing);

        foreach (var input in request.Allocations.Where(input => input.TargetPercentage > 0m))
        {
            _dbContext.TargetAllocations.Add(new TargetAllocation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CategoryId = input.CategoryId,
                TargetPercentage = input.TargetPercentage
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
