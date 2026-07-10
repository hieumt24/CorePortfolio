using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Analytics;

public record GetDividendAnalyticsQuery(int Months = 12, string Currency = "VND") : IRequest<List<DividendMonthlyAnalyticsDto>>;

public class DividendMonthlyAnalyticsDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class GetDividendAnalyticsHandler : IRequestHandler<GetDividendAnalyticsQuery, List<DividendMonthlyAnalyticsDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetDividendAnalyticsHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<DividendMonthlyAnalyticsDto>> Handle(GetDividendAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var startDate = DateTime.UtcNow.AddMonths(-request.Months);

        var transactions = await _dbContext.Transactions
            .Where(t => t.Portfolio != null && t.Portfolio.UserId == userId && t.Type == TransactionType.Dividend && t.Date >= startDate)
            .ToListAsync(cancellationToken);

        var grouped = transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new DividendMonthlyAnalyticsDto
            {
                Month = $"{g.Key.Month:D2}/{g.Key.Year}",
                Amount = g.Sum(t => t.Quantity * t.Price) // Dividend uses Quantity as number of shares and Price as dividend per share
            })
            .ToList();

        // Fill in missing months
        var result = new List<DividendMonthlyAnalyticsDto>();
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
                result.Add(new DividendMonthlyAnalyticsDto { Month = monthStr, Amount = 0 });
            }
        }

        return result;
    }
}
