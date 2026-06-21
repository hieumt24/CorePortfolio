using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.GetCashflowSummary;

public record CashflowSummaryDto(decimal TotalIncome, decimal TotalExpense, decimal NetFlow, List<CategorySummaryDto> IncomeByCategory, List<CategorySummaryDto> ExpenseByCategory);
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
        var userId = _currentUserService.UserId.Value;

        var query = _dbContext.CashflowRecords
            .Include(c => c.Category)
            .Where(c => c.UserId == userId && c.Currency == request.Currency);

        if (request.StartDate.HasValue)
        {
            query = query.Where(c => c.Date >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(c => c.Date <= request.EndDate.Value);
        }

        var records = await query.ToListAsync(cancellationToken);

        var totalIncome = records.Where(c => c.Category!.Type == CashflowType.Income).Sum(c => c.Amount);
        var totalExpense = records.Where(c => c.Category!.Type == CashflowType.Expense).Sum(c => c.Amount);
        var netFlow = totalIncome - totalExpense;

        var incomeByCategory = records.Where(c => c.Category!.Type == CashflowType.Income)
            .GroupBy(c => c.Category)
            .Select(g => new CategorySummaryDto(g.Key!.Name, g.Key.Icon, g.Key.Color, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var expenseByCategory = records.Where(c => c.Category!.Type == CashflowType.Expense)
            .GroupBy(c => c.Category)
            .Select(g => new CategorySummaryDto(g.Key!.Name, g.Key.Icon, g.Key.Color, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new CashflowSummaryDto(totalIncome, totalExpense, netFlow, incomeByCategory, expenseByCategory);
    }
}
