using System.Text.Json;
using CorePortfolio.API.Common;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Services;

public sealed record NotificationWriteRequest(
    Guid UserId,
    NotificationType Type,
    NotificationSeverity Severity,
    string Title,
    string Message,
    string DedupeKey,
    string? Link = null,
    string? EntityType = null,
    Guid? EntityId = null,
    string? ActionLabel = null,
    DateTime? ExpiresAt = null,
    IReadOnlyDictionary<string, string?>? Metadata = null);

public enum NotificationWriteOutcome
{
    Created,
    Duplicate,
    Suppressed
}

public sealed record NotificationWriteResult(
    NotificationWriteOutcome Outcome,
    Guid? NotificationId);

public sealed class NotificationWriter(
    AppDbContext dbContext,
    ILogger<NotificationWriter> logger)
{
    public async Task<NotificationWriteResult> WriteAsync(
        NotificationWriteRequest request,
        CancellationToken cancellationToken)
    {
        var queued = await QueueAsync(request, cancellationToken);
        if (queued.Outcome != NotificationWriteOutcome.Created ||
            !queued.NotificationId.HasValue)
            return queued;

        var notification = dbContext.Notifications.Local
            .Single(item => item.Id == queued.NotificationId.Value);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return queued;
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(notification).State = EntityState.Detached;
            var existingId = await dbContext.Notifications
                .AsNoTracking()
                .Where(item =>
                    item.UserId == request.UserId &&
                    item.DedupeKey == request.DedupeKey)
                .Select(item => (Guid?)item.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (existingId.HasValue)
                return new NotificationWriteResult(NotificationWriteOutcome.Duplicate, existingId);
            throw;
        }
    }

    public async Task<NotificationWriteResult> QueueAsync(
        NotificationWriteRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var isEnabled = await dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(preference => preference.UserId == request.UserId && preference.Type == request.Type)
            .Select(preference => (bool?)preference.IsEnabled)
            .SingleOrDefaultAsync(cancellationToken);
        if (isEnabled == false)
        {
            logger.LogDebug(
                "Notification {NotificationType} with dedupe key {DedupeKey} was suppressed by user preference",
                request.Type,
                request.DedupeKey);
            return new NotificationWriteResult(NotificationWriteOutcome.Suppressed, null);
        }

        var existingId = await dbContext.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.UserId == request.UserId &&
                notification.DedupeKey == request.DedupeKey)
            .Select(notification => (Guid?)notification.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (existingId.HasValue)
            return new NotificationWriteResult(NotificationWriteOutcome.Duplicate, existingId);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Type = request.Type,
            Severity = request.Severity,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Link = Normalize(request.Link),
            DedupeKey = request.DedupeKey.Trim(),
            EntityType = Normalize(request.EntityType),
            EntityId = request.EntityId,
            ActionLabel = Normalize(request.ActionLabel),
            MetadataJson = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt?.ToUniversalTime()
        };
        dbContext.Notifications.Add(notification);
        return new NotificationWriteResult(NotificationWriteOutcome.Created, notification.Id);
    }

    private static void Validate(NotificationWriteRequest request)
    {
        if (request.UserId == Guid.Empty)
            throw new RequestValidationException("Notification phải thuộc về một người dùng.");
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 160)
            throw new RequestValidationException("Tiêu đề thông báo phải có từ 1 đến 160 ký tự.");
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new RequestValidationException("Nội dung thông báo không được để trống.");
        if (string.IsNullOrWhiteSpace(request.DedupeKey) || request.DedupeKey.Trim().Length > 300)
            throw new RequestValidationException("Dedupe key phải có từ 1 đến 300 ký tự.");
        if (request.Link?.Trim().Length > 500)
            throw new RequestValidationException("Liên kết thông báo không được vượt quá 500 ký tự.");
        if (request.EntityType?.Trim().Length > 100)
            throw new RequestValidationException("Loại entity không được vượt quá 100 ký tự.");
        if (request.ActionLabel?.Trim().Length > 80)
            throw new RequestValidationException("Nhãn hành động không được vượt quá 80 ký tự.");
        if (request.ExpiresAt.HasValue)
        {
            if (request.ExpiresAt.Value.Kind != DateTimeKind.Utc)
                throw new RequestValidationException("Thời điểm hết hạn thông báo phải dùng UTC.");
            if (request.ExpiresAt <= DateTime.UtcNow)
                throw new RequestValidationException("Thời điểm hết hạn thông báo phải ở tương lai.");
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
