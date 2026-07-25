using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.Settings.GetNavigationSettings;

public class GetNavigationSettingsHandler(AppDbContext dbContext)
    : IRequestHandler<GetNavigationSettingsQuery, IReadOnlyList<NavigationFeatureDto>>
{
    public static readonly string[] FeatureKeys =
    [
        "NAV_DASHBOARD",
        "NAV_PORTFOLIOS",
        "NAV_TRANSACTIONS",
        "NAV_REPORTS",
        "NAV_CASHFLOW",
        "NAV_WATCHLIST",
        "NAV_BUDGETS",
        "NAV_SAVING_GOALS",
        "NAV_ANALYTICS",
        "NAV_REBALANCING",
        "NAV_DCA_PLANS"
    ];

    public async Task<IReadOnlyList<NavigationFeatureDto>> Handle(
        GetNavigationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var storedSettings = await dbContext.SystemSettings
            .AsNoTracking()
            .Where(setting => FeatureKeys.Contains(setting.Key))
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);

        return FeatureKeys
            .Select(key => new NavigationFeatureDto(
                key,
                !storedSettings.TryGetValue(key, out var value)
                || !bool.TryParse(value, out var isEnabled)
                || isEnabled))
            .ToArray();
    }
}
