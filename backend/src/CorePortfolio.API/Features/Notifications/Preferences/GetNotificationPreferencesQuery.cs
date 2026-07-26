using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Notifications.Preferences;

public sealed record GetNotificationPreferencesQuery : IRequest<List<NotificationPreferenceDto>>;

public sealed class GetNotificationPreferencesHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetNotificationPreferencesQuery, List<NotificationPreferenceDto>>
{
    public async Task<List<NotificationPreferenceDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var saved = await dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(preference => preference.UserId == userId)
            .ToDictionaryAsync(preference => preference.Type, cancellationToken);

        return Enum.GetValues<NotificationType>()
            .Select(type =>
            {
                if (saved.TryGetValue(type, out var preference))
                {
                    return new NotificationPreferenceDto(
                        type.ToString(),
                        preference.IsEnabled,
                        preference.WarningThreshold,
                        preference.CriticalThreshold,
                        preference.UpdatedAt);
                }

                var defaults = NotificationPreferenceDefaults.For(type);
                return new NotificationPreferenceDto(
                    type.ToString(),
                    true,
                    defaults.Warning,
                    defaults.Critical,
                    null);
            })
            .ToList();
    }
}
