using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.Overview;

public record AdminOverviewDto(
    int TotalUsers,
    int ActiveUsers,
    int AdminUsers,
    int TotalPortfolios,
    int TotalAssets,
    int TotalTransactions,
    int TotalCashflows,
    int TotalMarketAssets,
    int MarketAssetsNeedingAttention,
    DateTime GeneratedAt);

public record GetAdminOverviewQuery : IRequest<AdminOverviewDto>;

public sealed class GetAdminOverviewHandler(AppDbContext dbContext)
    : IRequestHandler<GetAdminOverviewQuery, AdminOverviewDto>
{
    public async Task<AdminOverviewDto> Handle(GetAdminOverviewQuery request, CancellationToken cancellationToken)
    {
        var staleBefore = DateTime.UtcNow.AddHours(-48);

        var totalUsers = await dbContext.Users.CountAsync(cancellationToken);
        var activeUsers = await dbContext.Users.CountAsync(user => user.IsActive, cancellationToken);
        var adminUsers = await dbContext.Users.CountAsync(user => user.Role == "Admin" && user.IsActive, cancellationToken);
        var totalPortfolios = await dbContext.Portfolios.CountAsync(cancellationToken);
        var totalAssets = await dbContext.Assets.CountAsync(cancellationToken);
        var totalTransactions = await dbContext.Transactions.CountAsync(cancellationToken);
        var totalCashflows = await dbContext.CashflowRecords.CountAsync(cancellationToken);
        var totalMarketAssets = await dbContext.MarketAssets.CountAsync(cancellationToken);
        var marketAssetsNeedingAttention = await dbContext.MarketAssets.CountAsync(asset =>
            asset.PriceStatus == "Error" || asset.PriceStatus == "Stale" ||
            (asset.PriceStatus == "Fresh" && asset.LastUpdated < staleBefore), cancellationToken);

        return new AdminOverviewDto(
            totalUsers,
            activeUsers,
            adminUsers,
            totalPortfolios,
            totalAssets,
            totalTransactions,
            totalCashflows,
            totalMarketAssets,
            marketAssetsNeedingAttention,
            DateTime.UtcNow);
    }
}
