using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Analytics;

public record GetCashflowHeatmapQuery() : IRequest<List<CashflowHeatmapDto>>;

public class CashflowHeatmapDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

public class GetCashflowHeatmapHandler : IRequestHandler<GetCashflowHeatmapQuery, List<CashflowHeatmapDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCashflowHeatmapHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<CashflowHeatmapDto>> Handle(GetCashflowHeatmapQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) throw new UnauthorizedAccessException();

        var startDate = DateTime.UtcNow.Date.AddDays(-365);

        var cashflows = await _dbContext.CashflowRecords
            .Where(c => c.UserId == userId && c.Date >= startDate)
            .GroupBy(c => c.Date.Date)
            .Select(g => new CashflowHeatmapDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                TotalAmount = g.Sum(c => c.Amount)
            })
            .ToListAsync(cancellationToken);

        return cashflows.OrderBy(c => c.Date).ToList();
    }
}
