using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CorePortfolio.API.Services;

public sealed class UserActivityOptions
{
    public const string SectionName = "UserActivity";

    public int OnlineWindowMinutes { get; set; } = 5;
    public int WriteIntervalSeconds { get; set; } = 60;
}

public interface IUserActivityService
{
    Task<bool> ValidateAccessAndTrackAsync(
        Guid userId,
        string? tokenRole,
        CancellationToken cancellationToken);
}

public sealed class UserActivityService(
    AppDbContext dbContext,
    IOptions<UserActivityOptions> options) : IUserActivityService
{
    public async Task<bool> ValidateAccessAndTrackAsync(
        Guid userId,
        string? tokenRole,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null || !user.IsActive || user.Role != tokenRole)
            return false;

        var now = DateTime.UtcNow;
        var writeIntervalSeconds = Math.Clamp(options.Value.WriteIntervalSeconds, 15, 300);
        var writeCutoff = now.AddSeconds(-writeIntervalSeconds);

        if (user.LastActivityAt is null || user.LastActivityAt < writeCutoff)
        {
            user.LastActivityAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}

public static class UserPresence
{
    public static DateTime GetOnlineCutoff(UserActivityOptions options, DateTime? now = null)
    {
        var onlineWindowMinutes = Math.Clamp(options.OnlineWindowMinutes, 1, 60);
        return (now ?? DateTime.UtcNow).AddMinutes(-onlineWindowMinutes);
    }
}
