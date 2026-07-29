using CorePortfolio.Domain.Analytics;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Analytics;

public class TargetAllocationDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TargetPercentage { get; set; }
}

public sealed record TargetAllocationPlanDto(
    IReadOnlyList<TargetAllocationDto> Allocations,
    decimal TotalPercentage,
    string Status,
    bool IsActionable,
    string? Reason);

public sealed record GetTargetAllocationsQuery : IRequest<TargetAllocationPlanDto>;

public class GetTargetAllocationsHandler : IRequestHandler<GetTargetAllocationsQuery, TargetAllocationPlanDto>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetTargetAllocationsHandler(
        AppDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<TargetAllocationPlanDto> Handle(
        GetTargetAllocationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var allocations = await _dbContext.TargetAllocations
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .Select(t => new TargetAllocationDto
            {
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                TargetPercentage = t.TargetPercentage
            })
            .ToListAsync(cancellationToken);

        var assessment = TargetAllocationPolicy.Evaluate(
            allocations.Select(allocation =>
                new TargetAllocationWeight(
                    allocation.CategoryId,
                    allocation.TargetPercentage)));
        var groupedAllocations = allocations
            .GroupBy(allocation => allocation.CategoryId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(allocation => allocation.TargetPercentage));
        var allCategories = await _dbContext.AssetCategories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
        
        var result = new List<TargetAllocationDto>();
        foreach (var cat in allCategories)
        {
            result.Add(new TargetAllocationDto
            {
                CategoryId = cat.Id,
                CategoryName = cat.Name,
                TargetPercentage = groupedAllocations.GetValueOrDefault(cat.Id)
            });
        }

        return new TargetAllocationPlanDto(
            result,
            assessment.TotalPercentage,
            assessment.Status,
            assessment.IsActionable,
            assessment.Reason);
    }
}
