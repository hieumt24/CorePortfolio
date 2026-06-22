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

public class GetTargetAllocationsQuery : IRequest<List<TargetAllocationDto>>
{
    public Guid UserId { get; set; }
    public GetTargetAllocationsQuery(Guid userId)
    {
        UserId = userId;
    }
}

public class GetTargetAllocationsHandler : IRequestHandler<GetTargetAllocationsQuery, List<TargetAllocationDto>>
{
    private readonly AppDbContext _dbContext;

    public GetTargetAllocationsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TargetAllocationDto>> Handle(GetTargetAllocationsQuery request, CancellationToken cancellationToken)
    {
        var allocations = await _dbContext.TargetAllocations
            .Include(t => t.Category)
            .Where(t => t.UserId == request.UserId)
            .Select(t => new TargetAllocationDto
            {
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                TargetPercentage = t.TargetPercentage
            })
            .ToListAsync(cancellationToken);

        var allCategories = await _dbContext.AssetCategories.ToListAsync(cancellationToken);
        
        var result = new List<TargetAllocationDto>();
        foreach (var cat in allCategories)
        {
            var existing = allocations.FirstOrDefault(a => a.CategoryId == cat.Id);
            if (existing != null)
            {
                result.Add(existing);
            }
            else
            {
                result.Add(new TargetAllocationDto
                {
                    CategoryId = cat.Id,
                    CategoryName = cat.Name,
                    TargetPercentage = 0
                });
            }
        }

        return result;
    }
}
