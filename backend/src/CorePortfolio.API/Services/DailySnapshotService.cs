using CorePortfolio.API.Features.Reports.TakeDailySnapshot;
using MediatR;

namespace CorePortfolio.API.Services;

public sealed class DailySnapshotService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailySnapshotService> _logger;
    private readonly TimeZoneInfo _timeZone;

    public DailySnapshotService(IServiceProvider serviceProvider, ILogger<DailySnapshotService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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

            for (var attempt = 1; attempt <= 3 && !stoppingToken.IsCancellationRequested; attempt++)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IMediator>()
                        .Send(new TakeDailySnapshotCommand(), stoppingToken);
                    break;
                }
                catch (Exception exception) when (attempt < 3)
                {
                    _logger.LogWarning(exception, "Daily snapshot attempt {Attempt} failed", attempt);
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
}
