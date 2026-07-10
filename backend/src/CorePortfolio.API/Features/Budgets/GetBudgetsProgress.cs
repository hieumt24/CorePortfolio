using CorePortfolio.Infrastructure.Data;
using CorePortfolio.API.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Budgets;

public record GetBudgetsProgressQuery(int? Year = null, int? Month = null, string Currency = "VND") : IRequest<List<BudgetProgressDto>>;

public class BudgetProgressDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal MonthlyLimit { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount => Math.Max(MonthlyLimit - SpentAmount, 0);
    public decimal RawProgressPercentage => MonthlyLimit > 0 ? (SpentAmount / MonthlyLimit) * 100 : 0;
    public decimal ProgressPercentage => Math.Min(RawProgressPercentage, 100);
    public bool IsExceeded => RawProgressPercentage >= 100;
    public string AlertLevel => RawProgressPercentage >= 100 ? "Exceeded" : RawProgressPercentage >= 80 ? "Warning" : "Healthy";
    public int Year { get; set; }
    public int Month { get; set; }
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
        var year = request.Year ?? now.Year;
        var month = request.Month ?? now.Month;
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(request.Month), "Month must be between 1 and 12.");

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "VND" : request.Currency.Trim().ToUpperInvariant();
        if (currency is not ("VND" or "USD")) throw new ArgumentException("Currency must be VND or USD.");

        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfNextMonth = startOfMonth.AddMonths(1);
        
        var budgets = await _dbContext.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);

        var allCategories = await _dbContext.CashflowCategories
            .AsNoTracking()
            .Where(c => c.IsGlobal || c.UserId == userId)
            .ToListAsync(cancellationToken);

        var budgetCategoryIds = budgets
            .SelectMany(b => GetCategoryAndChildrenIds(b.CategoryId, allCategories))
            .Distinct()
            .ToList();

        var spentAmounts = await _dbContext.CashflowRecords
            .Where(c => c.UserId == userId &&
                c.Date >= startOfMonth &&
                c.Date < startOfNextMonth &&
                c.Currency == currency &&
                budgetCategoryIds.Contains(c.CategoryId))
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
            SpentAmount = GetCategoryAndChildrenIds(b.CategoryId, allCategories).Sum(id => spentAmounts.GetValueOrDefault(id, 0m)),
            Year = year,
            Month = month
        }).ToList();
    }

    private static List<Guid> GetCategoryAndChildrenIds(Guid categoryId, List<CorePortfolio.Domain.Entities.CashflowCategory> categories)
    {
        var ids = new List<Guid> { categoryId };
        ids.AddRange(categories
            .Where(c => c.ParentCategoryId == categoryId)
            .Select(c => c.Id));
        return ids;
    }
}
