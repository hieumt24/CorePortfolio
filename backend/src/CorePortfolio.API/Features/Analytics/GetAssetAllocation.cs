using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using CorePortfolio.API.Features.Reports.GetGlobalReport;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Analytics;

public record GetAssetAllocationQuery(string Currency = "VND") : IRequest<List<AssetAllocationDto>>;

public class AssetAllocationDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public decimal Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
    public decimal TargetPercentage { get; set; }
    public decimal Deviation => Percentage - TargetPercentage;
}

public class GetAssetAllocationHandler : IRequestHandler<GetAssetAllocationQuery, List<AssetAllocationDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly ExchangeRateService _exchangeRateService;

    public GetAssetAllocationHandler(AppDbContext dbContext, ICurrentUserService currentUserService, IMediator mediator, ExchangeRateService exchangeRateService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<List<AssetAllocationDto>> Handle(GetAssetAllocationQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null || userId == Guid.Empty) throw new UnauthorizedAccessException();

        var report = await _mediator.Send(new GetGlobalReportQuery(userId.Value), cancellationToken);

        var vndUsdRate = await _exchangeRateService.GetUsdToVndAsync(cancellationToken);

        var result = new List<AssetAllocationDto>();

        var convertedAllocations = new List<AssetAllocationDto>();
        foreach (var cat in report.AllocationsByCategory)
        {
            var currentVal = cat.CurrentValue;
            if (request.Currency == "VND" && cat.Currency == "USD") currentVal *= vndUsdRate;
            else if (request.Currency == "USD" && cat.Currency == "VND") currentVal /= vndUsdRate;

            convertedAllocations.Add(new AssetAllocationDto
            {
                CategoryName = cat.CategoryName,
                TotalValue = currentVal
            });
        }

        // Group by category name (since now everything is in the same currency)
        var groupedAllocations = convertedAllocations
            .GroupBy(c => c.CategoryName)
            .Select(g => new AssetAllocationDto
            {
                CategoryName = g.Key,
                TotalValue = g.Sum(c => c.TotalValue)
            }).ToList();

        var totalValue = groupedAllocations.Sum(c => c.TotalValue);
        if (totalValue == 0) return result;

        // Hardcode colors or fetch from db
        var categoryColors = await _dbContext.AssetCategories.ToDictionaryAsync(c => c.Name, c => "#3b82f6", cancellationToken);
        // Using predefined colors for popular ones
        var defaultColors = new Dictionary<string, string>
        {
            { "Fiat", "#10b981" },
            { "Stock", "#3b82f6" },
            { "Crypto", "#f59e0b" },
            { "Real Estate", "#ef4444" }
        };

        var targets = await _dbContext.TargetAllocations
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var cat in groupedAllocations)
        {
            var color = defaultColors.ContainsKey(cat.CategoryName) ? defaultColors[cat.CategoryName] : "#8b5cf6";
            var targetObj = targets.FirstOrDefault(t => t.Category.Name == cat.CategoryName);
            var targetPct = targetObj?.TargetPercentage ?? 0m;
            
            result.Add(new AssetAllocationDto
            {
                CategoryName = cat.CategoryName,
                TotalValue = cat.TotalValue,
                Percentage = (cat.TotalValue / totalValue) * 100,
                Color = color,
                TargetPercentage = targetPct
            });
        }

        return result.OrderByDescending(r => r.Percentage).ToList();
    }
}
