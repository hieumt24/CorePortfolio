using CorePortfolio.Domain.Entities;

namespace CorePortfolio.API.Features.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Severity,
    string Title,
    string Message,
    string? Link,
    string? EntityType,
    Guid? EntityId,
    string? ActionLabel,
    string? MetadataJson,
    DateTime CreatedAt,
    DateTime? ReadAt,
    DateTime? DismissedAt,
    DateTime? ExpiresAt)
{
    public static NotificationDto FromEntity(Notification notification) => new(
        notification.Id,
        notification.Type.ToString(),
        notification.Severity.ToString(),
        notification.Title,
        notification.Message,
        notification.Link,
        notification.EntityType,
        notification.EntityId,
        notification.ActionLabel,
        notification.MetadataJson,
        notification.CreatedAt,
        notification.ReadAt,
        notification.DismissedAt,
        notification.ExpiresAt);
}

public sealed record NotificationPreferenceDto(
    string Type,
    bool IsEnabled,
    decimal? WarningThreshold,
    decimal? CriticalThreshold,
    DateTime? UpdatedAt);

public sealed record NotificationPreferenceInput(
    string Type,
    bool IsEnabled,
    decimal? WarningThreshold,
    decimal? CriticalThreshold);

public sealed record UpdateNotificationPreferencesRequest(List<NotificationPreferenceInput>? Preferences);

internal static class NotificationPreferenceDefaults
{
    public static (decimal? Warning, decimal? Critical) For(NotificationType type) => type switch
    {
        NotificationType.Budget => (80m, 100m),
        _ => (null, null)
    };
}
