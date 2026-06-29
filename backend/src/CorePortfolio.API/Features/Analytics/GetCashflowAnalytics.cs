using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Analytics;

public record GetCashflowAnalyticsQuery(int Months = 6, string Currency = "VND") : IRequest<List<CashflowMonthlyAnalyticsDto>>;

public class CashflowMonthlyAnalyticsDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Investment { get; set; }
    public decimal Saving { get; set; }
    public decimal NetFlow => Income - Expense;
}

public class GetCashflowAnalyticsHandler : IRequestHandler<GetCashflowAnalyticsQuery, List<CashflowMonthlyAnalyticsDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCashflowAnalyticsHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<CashflowMonthlyAnalyticsDto>> Handle(GetCashflowAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null || userId == Guid.Empty) throw new UnauthorizedAccessException();

        var startDate = DateTime.UtcNow.AddMonths(-request.Months);

        var cashflows = await _dbContext.CashflowRecords
            .Include(c => c.Category)
            .Where(c => c.UserId == userId && c.Date >= startDate && c.Currency == request.Currency)
            .ToListAsync(cancellationToken);

        var grouped = cashflows
            .GroupBy(c => new { c.Date.Year, c.Date.Month })
            .Select(g => new CashflowMonthlyAnalyticsDto
            {
                Month = $"{g.Key.Month:D2}/{g.Key.Year}",
                Income = g.Where(c => c.Category?.Type == CashflowType.Income).Sum(c => c.Amount),
                Expense = g.Where(c => c.Category?.Type == CashflowType.Expense).Sum(c => c.Amount),
                Investment = g.Where(c => c.Category?.Type == CashflowType.Investment).Sum(c => c.Amount),
                Saving = g.Where(c => c.Category?.Type == CashflowType.Saving).Sum(c => c.Amount)
            })
            .OrderBy(dto => dto.Month.Substring(3)) // Year
            .ThenBy(dto => dto.Month.Substring(0, 2)) // Month
            .ToList();

        // Fill in missing months
        var result = new List<CashflowMonthlyAnalyticsDto>();
        for (int i = request.Months - 1; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddMonths(-i);
            var monthStr = $"{date.Month:D2}/{date.Year}";
            var existing = grouped.FirstOrDefault(g => g.Month == monthStr);
            if (existing != null)
            {
                result.Add(existing);
            }
            else
            {
                result.Add(new CashflowMonthlyAnalyticsDto { Month = monthStr, Income = 0, Expense = 0, Investment = 0, Saving = 0 });
            }
        }

        return result;
    }
}
