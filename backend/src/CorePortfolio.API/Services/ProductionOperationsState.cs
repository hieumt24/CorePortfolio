using System.Collections.Concurrent;

namespace CorePortfolio.API.Services;

public sealed record BackgroundJobStatus(
    string Name,
    string State,
    DateTime? LastStartedAt,
    DateTime? LastSucceededAt,
    DateTime? LastFailedAt,
    long SuccessCount,
    long FailureCount,
    long? LastDurationMilliseconds,
    string? LastError);

public sealed record ProductionOperationsSnapshot(
    bool IsMaintenanceMode,
    string? MaintenanceReason,
    DateTime? MaintenanceStartedAt,
    IReadOnlyList<BackgroundJobStatus> Jobs);

public sealed class ProductionOperationsState
{
    private readonly ConcurrentDictionary<string, MutableJobStatus> _jobs =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _maintenanceLock = new();
    private string? _maintenanceReason;
    private DateTime? _maintenanceStartedAt;

    public bool IsMaintenanceMode
    {
        get
        {
            lock (_maintenanceLock)
                return _maintenanceStartedAt.HasValue;
        }
    }

    public void EnterMaintenance(string reason)
    {
        lock (_maintenanceLock)
        {
            _maintenanceReason = reason;
            _maintenanceStartedAt = DateTime.UtcNow;
        }
    }

    public void ExitMaintenance()
    {
        lock (_maintenanceLock)
        {
            _maintenanceReason = null;
            _maintenanceStartedAt = null;
        }
    }

    public DateTime StartJob(string name)
    {
        var startedAt = DateTime.UtcNow;
        _jobs.AddOrUpdate(
            name,
            _ => new MutableJobStatus { LastStartedAt = startedAt, State = "Running" },
            (_, status) =>
            {
                lock (status)
                {
                    status.LastStartedAt = startedAt;
                    status.State = "Running";
                }
                return status;
            });
        return startedAt;
    }

    public void CompleteJob(string name, DateTime startedAt)
    {
        var completedAt = DateTime.UtcNow;
        UpdateJob(name, status =>
        {
            status.State = "Succeeded";
            status.LastSucceededAt = completedAt;
            status.SuccessCount++;
            status.LastDurationMilliseconds = (long)(completedAt - startedAt).TotalMilliseconds;
            status.LastError = null;
        });
    }

    public void FailJob(string name, DateTime startedAt, Exception exception)
    {
        var failedAt = DateTime.UtcNow;
        UpdateJob(name, status =>
        {
            status.State = "Failed";
            status.LastFailedAt = failedAt;
            status.FailureCount++;
            status.LastDurationMilliseconds = (long)(failedAt - startedAt).TotalMilliseconds;
            status.LastError = exception.Message[..Math.Min(exception.Message.Length, 500)];
        });
    }

    public ProductionOperationsSnapshot GetSnapshot()
    {
        bool maintenance;
        string? reason;
        DateTime? startedAt;
        lock (_maintenanceLock)
        {
            maintenance = _maintenanceStartedAt.HasValue;
            reason = _maintenanceReason;
            startedAt = _maintenanceStartedAt;
        }

        var jobs = _jobs
            .Select(pair =>
            {
                lock (pair.Value)
                {
                    return new BackgroundJobStatus(
                        pair.Key,
                        pair.Value.State,
                        pair.Value.LastStartedAt,
                        pair.Value.LastSucceededAt,
                        pair.Value.LastFailedAt,
                        pair.Value.SuccessCount,
                        pair.Value.FailureCount,
                        pair.Value.LastDurationMilliseconds,
                        pair.Value.LastError);
                }
            })
            .OrderBy(item => item.Name)
            .ToArray();
        return new ProductionOperationsSnapshot(maintenance, reason, startedAt, jobs);
    }

    private void UpdateJob(string name, Action<MutableJobStatus> update)
    {
        var status = _jobs.GetOrAdd(name, _ => new MutableJobStatus());
        lock (status)
            update(status);
    }

    private sealed class MutableJobStatus
    {
        public string State { get; set; } = "NeverRun";
        public DateTime? LastStartedAt { get; set; }
        public DateTime? LastSucceededAt { get; set; }
        public DateTime? LastFailedAt { get; set; }
        public long SuccessCount { get; set; }
        public long FailureCount { get; set; }
        public long? LastDurationMilliseconds { get; set; }
        public string? LastError { get; set; }
    }
}
