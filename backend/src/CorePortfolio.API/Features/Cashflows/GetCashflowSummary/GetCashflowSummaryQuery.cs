using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.GetCashflowSummary;

public record CashflowSummaryDto(
    decimal TotalIncome, decimal TotalExpense,
    decimal TotalInvestment, decimal TotalSaving,
    decimal NetFlow,
    List<CategorySummaryDto> IncomeByCategory,
    List<CategorySummaryDto> ExpenseByCategory,
    List<CategorySummaryDto> InvestmentByCategory,
    List<CategorySummaryDto> SavingByCategory
);
public record CategorySummaryDto(string CategoryName, string Icon, string Color, decimal Amount);

public record GetCashflowSummaryQuery(string Currency = "VND", DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<CashflowSummaryDto>;

public class GetCashflowSummaryHandler : IRequestHandler<GetCashflowSummaryQuery, CashflowSummaryDto>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCashflowSummaryHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<CashflowSummaryDto> Handle(GetCashflowSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var query = _dbContext.CashflowRecords
            .Include(c => c.Category)
            .ThenInclude(c => c!.ParentCategory) // Include parent to group by parent
            .Where(c => c.UserId == userId && c.Currency == request.Currency);

        if (request.StartDate.HasValue)
        {
            query = query.Where(c => c.Date >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(c => c.Date <= request.EndDate.Value);
        }

        var records = (await query.ToListAsync(cancellationToken))
            .Where(c => c.Category != null)
            .ToList();

        var totalIncome = records.Where(c => c.Category!.Type == CashflowType.Income).Sum(c => c.Amount);
        var totalExpense = records.Where(c => c.Category!.Type == CashflowType.Expense).Sum(c => c.Amount);
        var totalInvestment = records.Where(c => c.Category!.Type == CashflowType.Investment).Sum(c => c.Amount);
        var totalSaving = records.Where(c => c.Category!.Type == CashflowType.Saving).Sum(c => c.Amount);
        var netFlow = totalIncome - totalExpense;

        var incomeByCategory = GroupByCategory(records, CashflowType.Income);
        var expenseByCategory = GroupByCategory(records, CashflowType.Expense);
        var investmentByCategory = GroupByCategory(records, CashflowType.Investment);
        var savingByCategory = GroupByCategory(records, CashflowType.Saving);

        return new CashflowSummaryDto(totalIncome, totalExpense, totalInvestment, totalSaving, netFlow, incomeByCategory, expenseByCategory, investmentByCategory, savingByCategory);
    }

    private List<CategorySummaryDto> GroupByCategory(List<CashflowRecord> records, CashflowType type)
    {
        return records
            .Where(c => c.Category!.Type == type)
            .GroupBy(c => c.Category!.ParentCategoryId.HasValue ? c.Category.ParentCategory! : c.Category!)
            .Select(g => new CategorySummaryDto(g.Key.Name, g.Key.Icon, g.Key.Color, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();
    }
}
