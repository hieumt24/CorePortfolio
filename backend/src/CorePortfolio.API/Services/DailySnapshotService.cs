using CorePortfolio.API.Features.Reports.TakeDailySnapshot;
using MediatR;

namespace CorePortfolio.API.Services;

public sealed class DailySnapshotService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailySnapshotService> _logger;
    private readonly ProductionOperationsState _operationsState;
    private readonly TimeZoneInfo _timeZone;

    public DailySnapshotService(
        IServiceProvider serviceProvider,
        ILogger<DailySnapshotService> logger,
        ProductionOperationsState operationsState)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _operationsState = operationsState;
        try { _timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (TimeZoneNotFoundException) { _timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
            var next = localNow.Date.AddHours(23).AddMinutes(55);
            if (next <= localNow) next = next.AddDays(1);
            await Task.Delay(next - localNow, stoppingToken);

            var startedAt = _operationsState.StartJob("DailySnapshot");
            for (var attempt = 1; attempt <= 3 && !stoppingToken.IsCancellationRequested; attempt++)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IMediator>()
                        .Send(new TakeDailySnapshotCommand(), stoppingToken);
                    _operationsState.CompleteJob("DailySnapshot", startedAt);
                    break;
                }
                catch (Exception exception) when (attempt < 3)
                {
                    _logger.LogWarning(exception, "Daily snapshot attempt {Attempt} failed", attempt);
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (Exception exception)
                {
                    _operationsState.FailJob("DailySnapshot", startedAt, exception);
                    _logger.LogError(exception, "Daily snapshot failed after {AttemptCount} attempts", attempt);
                }
            }
        }
    }
}
