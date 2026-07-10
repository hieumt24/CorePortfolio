using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.GetDailyCashflowSummary;

public record DailyCashflowSummaryDto(
    List<DaySummaryDto> Days,
    decimal MonthTotalIncome,
    decimal MonthTotalExpense,
    decimal MonthNetFlow,
    decimal DailyAverage
);

public record DaySummaryDto(
    DateTime Date,
    decimal Income,
    decimal Expense,
    decimal NetFlow,
    List<DayCategoryBreakdownDto> ExpenseBreakdown
);

public record DayCategoryBreakdownDto(string CategoryName, string Icon, string Color, decimal Amount);

public record GetDailyCashflowSummaryQuery(string Currency, string Month) : IRequest<DailyCashflowSummaryDto>;

public class GetDailyCashflowSummaryHandler : IRequestHandler<GetDailyCashflowSummaryQuery, DailyCashflowSummaryDto>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetDailyCashflowSummaryHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<DailyCashflowSummaryDto> Handle(GetDailyCashflowSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        if (!DateTime.TryParse(request.Month + "-01", out var startDate))
        {
            throw new ArgumentException("Invalid month format. Expected YYYY-MM.");
        }

        var endDate = startDate.AddMonths(1).AddDays(-1);

        var records = (await _dbContext.CashflowRecords
            .Include(c => c.Category)
            .ThenInclude(c => c!.ParentCategory)
            .Where(c => c.UserId == userId && c.Currency == request.Currency && c.Date >= startDate && c.Date <= endDate)
            .ToListAsync(cancellationToken))
            .Where(c => c.Category != null)
            .ToList();

        var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
        var days = new List<DaySummaryDto>();

        for (int i = 1; i <= daysInMonth; i++)
        {
            var date = new DateTime(startDate.Year, startDate.Month, i);
            var dayRecords = records.Where(r => r.Date.Date == date.Date).ToList();

            var income = dayRecords.Where(r => r.Category!.Type == CashflowType.Income).Sum(r => r.Amount);
            var expense = dayRecords.Where(r => r.Category!.Type == CashflowType.Expense).Sum(r => r.Amount);
            var netFlow = income - expense;

            var expenseBreakdown = dayRecords
                .Where(r => r.Category!.Type == CashflowType.Expense)
                .GroupBy(c => c.Category!.ParentCategoryId.HasValue ? c.Category.ParentCategory! : c.Category!)
                .Select(g => new DayCategoryBreakdownDto(g.Key.Name, g.Key.Icon, g.Key.Color, g.Sum(x => x.Amount)))
                .OrderByDescending(x => x.Amount)
                .Take(5)
                .ToList();

            days.Add(new DaySummaryDto(date, income, expense, netFlow, expenseBreakdown));
        }

        var monthTotalIncome = records.Where(r => r.Category!.Type == CashflowType.Income).Sum(r => r.Amount);
        var monthTotalExpense = records.Where(r => r.Category!.Type == CashflowType.Expense).Sum(r => r.Amount);
        var monthNetFlow = monthTotalIncome - monthTotalExpense;
        
        var daysPassed = (DateTime.UtcNow.Year == startDate.Year && DateTime.UtcNow.Month == startDate.Month)
            ? DateTime.UtcNow.Day
            : daysInMonth;
            
        var dailyAverage = daysPassed > 0 ? monthTotalExpense / daysPassed : 0;

        return new DailyCashflowSummaryDto(days, monthTotalIncome, monthTotalExpense, monthNetFlow, dailyAverage);
    }
}
