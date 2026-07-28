using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CorePortfolio.API.Features.Auth.TwoFactor;

public sealed class TwoFactorChallengeCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<TwoFactorOptions> options,
    ILogger<TwoFactorChallengeCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(
            Math.Clamp(options.Value.CleanupIntervalMinutes, 15, 1440));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var deleted = await CleanupOnceAsync(stoppingToken);
                if (deleted > 0)
                    logger.LogInformation(
                        "Deleted {Count} expired two-factor challenges.",
                        deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Two-factor challenge cleanup failed.");
            }
        }
    }

    public async Task<int> CleanupOnceAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddHours(
            -Math.Clamp(options.Value.ChallengeRetentionHours, 1, 168));
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.TwoFactorChallenges
            .Where(item =>
                item.ExpiresAt <= cutoff ||
                (item.ConsumedAt.HasValue && item.ConsumedAt <= cutoff))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
