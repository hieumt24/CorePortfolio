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

public class UpdateTargetAllocationsCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public List<TargetAllocationInput> Allocations { get; set; } = new();

    public UpdateTargetAllocationsCommand(Guid userId, List<TargetAllocationInput> allocations)
    {
        UserId = userId;
        Allocations = allocations;
    }
}

public class UpdateTargetAllocationsHandler : IRequestHandler<UpdateTargetAllocationsCommand, bool>
{
    private readonly AppDbContext _dbContext;

    public UpdateTargetAllocationsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateTargetAllocationsCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Total Percentage <= 100
        var totalPercentage = request.Allocations.Sum(a => a.TargetPercentage);
        if (totalPercentage > 100)
            throw new Exception("Tổng tỷ trọng mục tiêu không được vượt quá 100%");

        // 2. Fetch existing allocations
        var existing = await _dbContext.TargetAllocations
            .Where(t => t.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        // 3. Update or Add
        foreach (var input in request.Allocations)
        {
            var match = existing.FirstOrDefault(e => e.CategoryId == input.CategoryId);
            if (match != null)
            {
                match.TargetPercentage = input.TargetPercentage;
            }
            else
            {
                _dbContext.TargetAllocations.Add(new TargetAllocation
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    CategoryId = input.CategoryId,
                    TargetPercentage = input.TargetPercentage
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
