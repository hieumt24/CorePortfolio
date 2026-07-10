using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.GetMonthlyCashflowReport;

public record MonthlyCashflowReportDto(
    List<MonthSummaryDto> Months,
    decimal YearTotalIncome,
    decimal YearTotalExpense,
    decimal YearNetFlow,
    List<CategoryTrendDto> CategoryTrends
);

public record MonthSummaryDto(
    int Month, int Year,
    decimal Income, decimal Expense,
    decimal Investment, decimal Saving,
    decimal NetFlow
);

public record CategoryTrendDto(
    string CategoryName, string Icon, string Color,
    List<decimal> MonthlyAmounts
);

public record GetMonthlyCashflowReportQuery(string Currency, int Year) : IRequest<MonthlyCashflowReportDto>;

public class GetMonthlyCashflowReportHandler : IRequestHandler<GetMonthlyCashflowReportQuery, MonthlyCashflowReportDto>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetMonthlyCashflowReportHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<MonthlyCashflowReportDto> Handle(GetMonthlyCashflowReportQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var startDate = new DateTime(request.Year, 1, 1);
        var endDate = new DateTime(request.Year, 12, 31);

        var records = (await _dbContext.CashflowRecords
            .Include(c => c.Category)
            .ThenInclude(c => c!.ParentCategory)
            .Where(c => c.UserId == userId && c.Currency == request.Currency && c.Date >= startDate && c.Date <= endDate)
            .ToListAsync(cancellationToken))
            .Where(c => c.Category != null)
            .ToList();

        var months = new List<MonthSummaryDto>();

        for (int i = 1; i <= 12; i++)
        {
            var monthRecords = records.Where(r => r.Date.Month == i).ToList();

            var income = monthRecords.Where(r => r.Category!.Type == CashflowType.Income).Sum(r => r.Amount);
            var expense = monthRecords.Where(r => r.Category!.Type == CashflowType.Expense).Sum(r => r.Amount);
            var investment = monthRecords.Where(r => r.Category!.Type == CashflowType.Investment).Sum(r => r.Amount);
            var saving = monthRecords.Where(r => r.Category!.Type == CashflowType.Saving).Sum(r => r.Amount);
            var netFlow = income - expense;

            months.Add(new MonthSummaryDto(i, request.Year, income, expense, investment, saving, netFlow));
        }

        var yearTotalIncome = records.Where(r => r.Category!.Type == CashflowType.Income).Sum(r => r.Amount);
        var yearTotalExpense = records.Where(r => r.Category!.Type == CashflowType.Expense).Sum(r => r.Amount);
        var yearNetFlow = yearTotalIncome - yearTotalExpense;

        var categoryTrends = records
            .Where(r => r.Category!.Type == CashflowType.Expense)
            .GroupBy(c => c.Category!.ParentCategoryId.HasValue ? c.Category.ParentCategory! : c.Category!)
            .Select(g => 
            {
                var monthlyAmounts = new List<decimal>();
                for (int i = 1; i <= 12; i++)
                {
                    monthlyAmounts.Add(g.Where(r => r.Date.Month == i).Sum(r => r.Amount));
                }
                return new CategoryTrendDto(g.Key.Name, g.Key.Icon, g.Key.Color, monthlyAmounts);
            })
            .OrderByDescending(x => x.MonthlyAmounts.Sum())
            .ToList();

        return new MonthlyCashflowReportDto(months, yearTotalIncome, yearTotalExpense, yearNetFlow, categoryTrends);
    }
}
