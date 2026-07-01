using CorePortfolio.Infrastructure.Data;
using CorePortfolio.API.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Budgets;

public record GetBudgetsProgressQuery() : IRequest<List<BudgetProgressDto>>;

public class BudgetProgressDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal MonthlyLimit { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal ProgressPercentage => MonthlyLimit > 0 ? Math.Min((SpentAmount / MonthlyLimit) * 100, 100) : 0;
}

public class GetBudgetsProgressHandler : IRequestHandler<GetBudgetsProgressQuery, List<BudgetProgressDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetBudgetsProgressHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<BudgetProgressDto>> Handle(GetBudgetsProgressQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId.Value;
        
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        
        var budgets = await _dbContext.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);
            
        var categoryIds = budgets.Select(b => b.CategoryId).ToList();
        
        var spentAmounts = await _dbContext.CashflowRecords
            .Where(c => c.UserId == userId && c.Date >= startOfMonth && categoryIds.Contains(c.CategoryId))
            .GroupBy(c => c.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(c => c.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total, cancellationToken);
            
        return budgets.Select(b => new BudgetProgressDto
        {
            Id = b.Id,
            CategoryId = b.CategoryId,
            CategoryName = b.Category.Name,
            CategoryIcon = b.Category.Icon ?? "",
            CategoryColor = b.Category.Color ?? "",
            MonthlyLimit = b.MonthlyLimit,
            SpentAmount = spentAmounts.GetValueOrDefault(b.CategoryId, 0m)
        }).ToList();
    }
}
