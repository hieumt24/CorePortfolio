using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Notifications.Preferences;

public sealed record UpdateNotificationPreferencesCommand(List<NotificationPreferenceInput>? Preferences)
    : IRequest<List<NotificationPreferenceDto>>;

public sealed class UpdateNotificationPreferencesHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateNotificationPreferencesCommand, List<NotificationPreferenceDto>>
{
    public async Task<List<NotificationPreferenceDto>> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        if (request.Preferences is null)
            throw new RequestValidationException("Danh sách cấu hình thông báo là bắt buộc.");
        var parsedPreferences = ParseAndValidate(request.Preferences);
        var requestedTypes = parsedPreferences.Select(input => input.Type).ToList();
        var saved = await dbContext.NotificationPreferences
            .Where(preference => preference.UserId == userId && requestedTypes.Contains(preference.Type))
            .ToDictionaryAsync(preference => preference.Type, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var input in parsedPreferences)
        {
            if (!saved.TryGetValue(input.Type, out var preference))
            {
                preference = new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = input.Type
                };
                dbContext.NotificationPreferences.Add(preference);
                saved[input.Type] = preference;
            }

            preference.IsEnabled = input.IsEnabled;
            preference.WarningThreshold = input.WarningThreshold;
            preference.CriticalThreshold = input.CriticalThreshold;
            preference.UpdatedAt = now;
        }

        if (parsedPreferences.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        var allSaved = await dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(preference => preference.UserId == userId)
            .ToDictionaryAsync(preference => preference.Type, cancellationToken);
        return Enum.GetValues<NotificationType>()
            .Select(type =>
            {
                if (allSaved.TryGetValue(type, out var preference))
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

    private static List<ParsedPreference> ParseAndValidate(
        IReadOnlyCollection<NotificationPreferenceInput> preferences)
    {
        var parsed = new List<ParsedPreference>(preferences.Count);

        foreach (var preference in preferences)
        {
            if (!Enum.TryParse<NotificationType>(preference.Type, true, out var type) ||
                !Enum.IsDefined(type))
            {
                throw new RequestValidationException($"Loại thông báo '{preference.Type}' không hợp lệ.");
            }
            if (preference.WarningThreshold < 0 || preference.CriticalThreshold < 0)
                throw new RequestValidationException("Ngưỡng cảnh báo không được nhỏ hơn 0.");
            if (preference.WarningThreshold.HasValue &&
                preference.CriticalThreshold.HasValue &&
                preference.CriticalThreshold < preference.WarningThreshold)
            {
                throw new RequestValidationException("Ngưỡng nghiêm trọng phải lớn hơn hoặc bằng ngưỡng cảnh báo.");
            }

            parsed.Add(new ParsedPreference(
                type,
                preference.IsEnabled,
                preference.WarningThreshold,
                preference.CriticalThreshold));
        }

        if (parsed.GroupBy(preference => preference.Type).Any(group => group.Count() > 1))
            throw new RequestValidationException("Mỗi loại thông báo chỉ được cấu hình một lần.");
        return parsed;
    }

    private sealed record ParsedPreference(
        NotificationType Type,
        bool IsEnabled,
        decimal? WarningThreshold,
        decimal? CriticalThreshold);
}
