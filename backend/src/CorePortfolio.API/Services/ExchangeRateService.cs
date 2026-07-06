using CorePortfolio.API.Common;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Services;

public sealed class ExchangeRateService
{
    public const string UsdToVndKey = "USD_TO_VND";
    private readonly AppDbContext _dbContext;

    public ExchangeRateService(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<decimal> GetUsdToVndAsync(CancellationToken cancellationToken)
    {
        var value = await _dbContext.SystemSettings.AsNoTracking()
            .Where(s => s.Key == UsdToVndKey)
            .Select(s => s.Value)
            .SingleOrDefaultAsync(cancellationToken);
        if (!decimal.TryParse(value, out var rate) || rate <= 0)
            throw new ResourceConflictException("Tỷ giá USD/VND chưa được cấu hình hợp lệ.");
        return rate;
    }

    public static decimal ToVnd(decimal amount, string currency, decimal usdToVnd) =>
        currency.Equals("USD", StringComparison.OrdinalIgnoreCase) ? amount * usdToVnd : amount;
}
