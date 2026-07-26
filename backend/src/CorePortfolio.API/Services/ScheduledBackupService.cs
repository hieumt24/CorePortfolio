using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Services;

public sealed class ScheduledBackupService(
    IServiceScopeFactory scopeFactory,
    ProductionOperationsState operationsState,
    ILogger<ScheduledBackupService> logger) : BackgroundService
{
    private DateOnly? _lastCompletedDate;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Scheduled backup evaluation failed.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                return;
        }
    }

    private async Task EvaluateAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await db.SystemSettings.AsNoTracking()
            .Where(item => item.Key == "BACKUP_SCHEDULE_ENABLED" || item.Key == "BACKUP_SCHEDULE_UTC")
            .ToDictionaryAsync(item => item.Key, item => item.Value, cancellationToken);
        if (!settings.TryGetValue("BACKUP_SCHEDULE_ENABLED", out var enabled) ||
            !bool.TryParse(enabled, out var isEnabled) || !isEnabled)
            return;
        if (!settings.TryGetValue("BACKUP_SCHEDULE_UTC", out var configuredTime) ||
            !TimeOnly.TryParse(configuredTime, out var schedule))
            return;

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        if (_lastCompletedDate == today || TimeOnly.FromDateTime(now) < schedule)
            return;

        var startedAt = operationsState.StartJob("Scheduled Database Backup");
        try
        {
            var backupService = scope.ServiceProvider.GetRequiredService<BackupService>();
            await backupService.CreateBackupAsync(cancellationToken);
            operationsState.CompleteJob("Scheduled Database Backup", startedAt);
            _lastCompletedDate = today;
        }
        catch (Exception exception)
        {
            operationsState.FailJob("Scheduled Database Backup", startedAt, exception);
            throw;
        }
    }
}
