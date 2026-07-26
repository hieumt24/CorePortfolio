using System.Text.Json;
using CorePortfolio.API.Features.MarketAssets.UpdateMarketAssetPrice;
using CorePortfolio.API.Features.Reports.TakeDailySnapshot;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.ControlPlane;

public sealed record GetAdminCapabilitiesQuery : IRequest<object>;
public sealed class GetAdminCapabilitiesHandler(ICurrentUserService currentUser) :
    IRequestHandler<GetAdminCapabilitiesQuery, object>
{
    public Task<object> Handle(GetAdminCapabilitiesQuery request, CancellationToken cancellationToken) =>
        Task.FromResult<object>(new
        {
            role = currentUser.Role,
            roles = AdminPermissionCatalog.Roles,
            permissions = AdminPermissionCatalog.GetForRole(currentUser.Role)
        });
}

public sealed record GetAuditEventsQuery(
    string? Search, string? Action, string? EntityType, string? Outcome, Guid? ActorUserId,
    string? IpAddress, DateTime? From, DateTime? To, int Page, int PageSize) : IRequest<object>;
public sealed class GetAuditEventsHandler(AppDbContext db) : IRequestHandler<GetAuditEventsQuery, object>
{
    public async Task<object> Handle(GetAuditEventsQuery request, CancellationToken cancellationToken)
    {
        var query = db.AuditEvents.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var value = request.Search.Trim();
            query = query.Where(item =>
                item.Action.Contains(value) || item.EntityType.Contains(value) ||
                (item.EntityId != null && item.EntityId.Contains(value)) ||
                (item.CorrelationId != null && item.CorrelationId.Contains(value)));
        }
        if (!string.IsNullOrWhiteSpace(request.Action)) query = query.Where(x => x.Action == request.Action);
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(x => x.EntityType == request.EntityType);
        if (!string.IsNullOrWhiteSpace(request.Outcome)) query = query.Where(x => x.Outcome == request.Outcome);
        if (request.ActorUserId.HasValue) query = query.Where(x => x.ActorUserId == request.ActorUserId);
        if (!string.IsNullOrWhiteSpace(request.IpAddress)) query = query.Where(x => x.IpAddress == request.IpAddress);
        if (request.From.HasValue) query = query.Where(x => x.OccurredAt >= request.From.Value.ToUniversalTime());
        if (request.To.HasValue) query = query.Where(x => x.OccurredAt <= request.To.Value.ToUniversalTime());
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, x.ActorUserId, actorUsername = x.ActorUserId == null ? null :
                    db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.Username).FirstOrDefault(),
                x.Action, x.EntityType, x.EntityId, x.Outcome, x.IpAddress,
                x.CorrelationId, x.MetadataJson, x.OccurredAt
            }).ToListAsync(cancellationToken);
        return new { items, total, page, pageSize };
    }
}

public sealed record GetAdminUserDetailQuery(Guid UserId) : IRequest<object?>;
public sealed class GetAdminUserDetailHandler(AppDbContext db, IConfiguration configuration) :
    IRequestHandler<GetAdminUserDetailQuery, object?>
{
    public async Task<object?> Handle(GetAdminUserDetailQuery request, CancellationToken cancellationToken)
    {
        var onlineCutoff = DateTime.UtcNow.AddMinutes(-Math.Clamp(
            configuration.GetValue("UserActivity:OnlineWindowMinutes", 5), 1, 60));
        return await db.Users.AsNoTracking().Where(x => x.Id == request.UserId).Select(x => new
        {
            x.Id, x.Username, x.DisplayName, x.Email, x.Role, x.IsActive, x.CreatedAt,
            x.LastLoginAt, x.LastLoginIpAddress, x.LastActivityAt,
            IsOnline = x.LastActivityAt >= onlineCutoff,
            PortfolioCount = x.Portfolios.Count,
            TransactionCount = x.Portfolios.SelectMany(p => p.Transactions).Count(),
            CashflowCount = x.CashflowRecords.Count,
            ActiveSessionCount = x.Sessions.Count(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
        }).SingleOrDefaultAsync(cancellationToken);
    }
}

public sealed record GetUserSessionsQuery(Guid UserId) : IRequest<object>;
public sealed class GetUserSessionsHandler(AppDbContext db) : IRequestHandler<GetUserSessionsQuery, object>
{
    public async Task<object> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken) =>
        await db.UserSessions.AsNoTracking().Where(x => x.UserId == request.UserId)
            .OrderByDescending(x => x.LastSeenAt).Select(x => new
            {
                x.Id, x.IpAddress, x.UserAgent, x.CreatedAt, x.LastSeenAt, x.ExpiresAt,
                x.RevokedAt, x.RevokeReason, IsActive = x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow
            }).ToListAsync(cancellationToken);
}

public sealed record RevokeUserSessionsCommand(Guid UserId, Guid? SessionId, string Reason) : IRequest<int>;
public sealed class RevokeUserSessionsHandler(
    AppDbContext db, ICurrentUserService currentUser, AuditWriter audit) :
    IRequestHandler<RevokeUserSessionsCommand, int>
{
    public async Task<int> Handle(RevokeUserSessionsCommand request, CancellationToken cancellationToken)
    {
        if (!AdminPermissionCatalog.Has(currentUser.Role, "Sessions.Revoke")) throw new UnauthorizedAccessException();
        var sessions = await db.UserSessions.Where(x => x.UserId == request.UserId &&
            x.RevokedAt == null && (!request.SessionId.HasValue || x.Id == request.SessionId))
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedByUserId = currentUser.UserId;
            session.RevokeReason = string.IsNullOrWhiteSpace(request.Reason) ? "Revoked by administrator" : request.Reason.Trim();
        }
        audit.Add("UserSessionsRevoked", "User", request.UserId.ToString(),
            new { request.SessionId, count = sessions.Count });
        await db.SaveChangesAsync(cancellationToken);
        return sessions.Count;
    }
}

public sealed record GetSecurityTimelineQuery(Guid UserId, int Limit = 50) : IRequest<object>;
public sealed class GetSecurityTimelineHandler(AppDbContext db) : IRequestHandler<GetSecurityTimelineQuery, object>
{
    public async Task<object> Handle(GetSecurityTimelineQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId.ToString();
        return await db.AuditEvents.AsNoTracking()
            .Where(x => x.ActorUserId == request.UserId || (x.EntityType == "User" && x.EntityId == userId))
            .OrderByDescending(x => x.OccurredAt).Take(Math.Clamp(request.Limit, 1, 100))
            .Select(x => new { x.Id, x.Action, x.Outcome, x.IpAddress, x.MetadataJson, x.OccurredAt })
            .ToListAsync(cancellationToken);
    }
}

public sealed record UpdateAdminRoleCommand(Guid UserId, string Role) : IRequest<bool>;
public sealed class UpdateAdminRoleHandler(
    AppDbContext db, ICurrentUserService currentUser, AuditWriter audit) :
    IRequestHandler<UpdateAdminRoleCommand, bool>
{
    public async Task<bool> Handle(UpdateAdminRoleCommand request, CancellationToken cancellationToken)
    {
        if (!AdminPermissionCatalog.Has(currentUser.Role, "Roles.Manage")) throw new UnauthorizedAccessException();
        if (!AdminPermissionCatalog.Roles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Vai trò không hợp lệ.");
        if (currentUser.UserId == request.UserId && request.Role is not ("Admin" or "SuperAdmin"))
            throw new InvalidOperationException("Không thể tự loại bỏ quyền quản trị.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return false;
        var previousRole = user.Role;
        if (previousRole is "Admin" or "SuperAdmin" &&
            request.Role is not ("Admin" or "SuperAdmin") &&
            !await db.Users.AnyAsync(x => x.Id != user.Id && x.IsActive &&
                (x.Role == "Admin" || x.Role == "SuperAdmin"), cancellationToken))
            throw new InvalidOperationException("Phải duy trì ít nhất một quản trị viên đang hoạt động.");
        user.Role = request.Role;
        foreach (var session in await db.UserSessions.Where(x => x.UserId == user.Id && x.RevokedAt == null).ToListAsync(cancellationToken))
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedByUserId = currentUser.UserId;
            session.RevokeReason = "Role changed";
        }
        audit.Add("UserRoleChanged", "User", user.Id.ToString(), new { previousRole, user.Role });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record GetMarketDataHealthQuery : IRequest<object>;
public sealed class GetMarketDataHealthHandler(AppDbContext db) : IRequestHandler<GetMarketDataHealthQuery, object>
{
    public async Task<object> Handle(GetMarketDataHealthQuery request, CancellationToken cancellationToken)
    {
        var providers = await db.MarketAssets.AsNoTracking().GroupBy(x => x.PriceSource)
            .Select(g => new
            {
                provider = g.Key, total = g.Count(), fresh = g.Count(x => x.PriceStatus == "Fresh"),
                stale = g.Count(x => x.PriceStatus == "Stale"), errors = g.Count(x => x.PriceStatus == "Error"),
                lastUpdated = g.Max(x => x.LastUpdated)
            }).OrderBy(x => x.provider).ToListAsync(cancellationToken);
        var attention = await db.MarketAssets.AsNoTracking()
            .Where(x => x.PriceStatus == "Stale" || x.PriceStatus == "Error" || x.CurrentPrice <= 0)
            .OrderByDescending(x => x.LastUpdated).Take(100)
            .Select(x => new { x.Id, x.Symbol, x.Name, x.PriceSource, x.PriceStatus, x.LastUpdated, x.LastPriceError })
            .ToListAsync(cancellationToken);
        return new { providers, attention, generatedAt = DateTime.UtcNow };
    }
}

public sealed record RunAdminJobCommand(string JobName) : IRequest<object>;
public sealed class RunAdminJobHandler(
    ISender sender, ProductionOperationsState operations, ICurrentUserService currentUser, AuditWriter audit,
    AppDbContext db) : IRequestHandler<RunAdminJobCommand, object>
{
    public async Task<object> Handle(RunAdminJobCommand request, CancellationToken cancellationToken)
    {
        if (!AdminPermissionCatalog.Has(currentUser.Role, "Operations.Execute")) throw new UnauthorizedAccessException();
        var name = request.JobName.Trim().ToLowerInvariant();
        var startedAt = operations.StartJob($"Manual:{name}");
        try
        {
            object result = name switch
            {
                "daily-snapshot" => await sender.Send(new TakeDailySnapshotCommand(), cancellationToken),
                "market-price-refresh" => await sender.Send(new RefreshMarketAssetPricesCommand(), cancellationToken),
                _ => throw new ArgumentException("Job không được hỗ trợ.")
            };
            operations.CompleteJob($"Manual:{name}", startedAt);
            audit.Add("AdminJobExecuted", "BackgroundJob", name);
            await db.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            operations.FailJob($"Manual:{name}", startedAt, exception);
            throw;
        }
    }
}

public sealed record BroadcastNotificationCommand(
    string Title, string Message, NotificationSeverity Severity, string? Role,
    string? Link, DateTime? ExpiresAt) : IRequest<int>;
public sealed class BroadcastNotificationHandler(
    AppDbContext db, ICurrentUserService currentUser, AuditWriter audit) :
    IRequestHandler<BroadcastNotificationCommand, int>
{
    public async Task<int> Handle(BroadcastNotificationCommand request, CancellationToken cancellationToken)
    {
        if (!AdminPermissionCatalog.Has(currentUser.Role, "Notifications.Manage")) throw new UnauthorizedAccessException();
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Tiêu đề và nội dung là bắt buộc.");
        var users = db.Users.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Role)) users = users.Where(x => x.Role == request.Role);
        var userIds = await users.Select(x => x.Id).ToListAsync(cancellationToken);
        var campaignId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        db.Notifications.AddRange(userIds.Select(userId => new Notification
        {
            UserId = userId, Type = NotificationType.System, Severity = request.Severity,
            Title = request.Title.Trim(), Message = request.Message.Trim(), Link = request.Link,
            DedupeKey = $"admin:broadcast:{campaignId:N}:{userId:N}", EntityType = "AdminBroadcast",
            EntityId = campaignId, ActionLabel = string.IsNullOrWhiteSpace(request.Link) ? null : "Xem chi tiết",
            ExpiresAt = request.ExpiresAt, CreatedAt = createdAt,
            MetadataJson = JsonSerializer.Serialize(new { campaignId })
        }));
        audit.Add("NotificationBroadcastCreated", "AdminBroadcast", campaignId.ToString(),
            new { recipients = userIds.Count, request.Role, request.Severity });
        await db.SaveChangesAsync(cancellationToken);
        return userIds.Count;
    }
}

public sealed record GetNotificationCampaignsQuery(int Limit = 50) : IRequest<object>;
public sealed class GetNotificationCampaignsHandler(AppDbContext db) : IRequestHandler<GetNotificationCampaignsQuery, object>
{
    public async Task<object> Handle(GetNotificationCampaignsQuery request, CancellationToken cancellationToken) =>
        await db.Notifications.AsNoTracking().Where(x => x.EntityType == "AdminBroadcast")
            .GroupBy(x => new { x.EntityId, x.Title, x.Message, x.Severity, x.CreatedAt, x.ExpiresAt })
            .OrderByDescending(x => x.Key.CreatedAt).Take(Math.Clamp(request.Limit, 1, 100))
            .Select(x => new
            {
                id = x.Key.EntityId, x.Key.Title, x.Key.Message, x.Key.Severity, x.Key.CreatedAt,
                x.Key.ExpiresAt, recipientCount = x.Count(), readCount = x.Count(n => n.ReadAt != null)
            }).ToListAsync(cancellationToken);
}

public sealed record GetDataIntegrityQuery : IRequest<object>;
public sealed class GetDataIntegrityHandler(AppDbContext db) : IRequestHandler<GetDataIntegrityQuery, object>
{
    public async Task<object> Handle(GetDataIntegrityQuery request, CancellationToken cancellationToken)
    {
        var checks = new List<object>
        {
            Check("market-assets-without-price", "Market Asset thiếu giá", await db.MarketAssets.CountAsync(x => x.CurrentPrice <= 0, cancellationToken), "Warning"),
            Check("stale-market-assets", "Market Asset stale/error", await db.MarketAssets.CountAsync(x => x.PriceStatus == "Stale" || x.PriceStatus == "Error", cancellationToken), "Warning"),
            Check("unclassified-ledger", "Ledger chưa phân loại", await db.CashLedgerEntries.CountAsync(x => x.Classification == CashLedgerEntryClassification.Unknown, cancellationToken), "Critical"),
            Check("orphan-assets", "Asset không có Market Asset", await db.Assets.CountAsync(x => x.MarketAsset == null, cancellationToken), "Critical"),
            Check("missing-snapshots", "Portfolio chưa có snapshot", await db.Portfolios.CountAsync(x => !x.Snapshots.Any(), cancellationToken), "Warning"),
            Check("expired-sessions", "Session hết hạn chưa dọn", await db.UserSessions.CountAsync(x => x.ExpiresAt < DateTime.UtcNow && x.RevokedAt == null, cancellationToken), "Info")
        };
        return new { checks, generatedAt = DateTime.UtcNow };
    }

    private static object Check(string key, string label, int count, string severity) =>
        new { key, label, count, severity, status = count == 0 ? "Healthy" : "Attention" };
}

public sealed record RepairDataIntegrityCommand(string CheckKey, bool DryRun) : IRequest<object>;
public sealed class RepairDataIntegrityHandler(
    AppDbContext db, ICurrentUserService currentUser, AuditWriter audit) :
    IRequestHandler<RepairDataIntegrityCommand, object>
{
    public async Task<object> Handle(RepairDataIntegrityCommand request, CancellationToken cancellationToken)
    {
        if (!AdminPermissionCatalog.Has(currentUser.Role, "Integrity.Repair")) throw new UnauthorizedAccessException();
        if (request.CheckKey != "expired-sessions") throw new ArgumentException("Kiểm tra này chỉ hỗ trợ hướng dẫn xử lý thủ công.");
        var rows = await db.UserSessions.Where(x => x.ExpiresAt < DateTime.UtcNow && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        if (!request.DryRun)
        {
            foreach (var row in rows) { row.RevokedAt = DateTime.UtcNow; row.RevokeReason = "Expired session cleanup"; }
            audit.Add("DataIntegrityRepairExecuted", "UserSession", null, new { request.CheckKey, count = rows.Count });
            await db.SaveChangesAsync(cancellationToken);
        }
        return new { request.CheckKey, request.DryRun, affected = rows.Count };
    }
}

public sealed record GetAdminSystemConfigurationQuery : IRequest<object>;
public sealed class GetAdminSystemConfigurationHandler(AppDbContext db, IConfiguration configuration) :
    IRequestHandler<GetAdminSystemConfigurationQuery, object>
{
    public async Task<object> Handle(GetAdminSystemConfigurationQuery request, CancellationToken cancellationToken)
    {
        var values = await db.SystemSettings.AsNoTracking()
            .Where(x => x.Key.StartsWith("ADMIN_") || x.Key.StartsWith("BACKUP_"))
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        return new
        {
            settings = values,
            runtime = new
            {
                backupDirectoryConfigured = !string.IsNullOrWhiteSpace(configuration["Backups:Directory"]),
                retentionCount = configuration.GetValue("Backups:RetentionCount", 10),
                databaseProvider = "SQLite"
            }
        };
    }
}

public sealed record UpdateAdminSystemConfigurationCommand(Dictionary<string, string> Settings) : IRequest<bool>;
public sealed class UpdateAdminSystemConfigurationHandler(
    AppDbContext db, ICurrentUserService currentUser, AuditWriter audit) :
    IRequestHandler<UpdateAdminSystemConfigurationCommand, bool>
{
    private static readonly HashSet<string> Allowed =
        ["ADMIN_ANNOUNCEMENTS_ENABLED", "ADMIN_MAINTENANCE_BANNER", "BACKUP_SCHEDULE_ENABLED", "BACKUP_SCHEDULE_UTC"];

    public async Task<bool> Handle(UpdateAdminSystemConfigurationCommand request, CancellationToken cancellationToken)
    {
        if (!AdminPermissionCatalog.Has(currentUser.Role, "Settings.Manage")) throw new UnauthorizedAccessException();
        foreach (var pair in request.Settings.Where(x => Allowed.Contains(x.Key)))
        {
            var setting = await db.SystemSettings.SingleOrDefaultAsync(x => x.Key == pair.Key, cancellationToken);
            if (setting is null)
                db.SystemSettings.Add(new SystemSetting { Key = pair.Key, Value = pair.Value, Description = "Admin control plane setting" });
            else { setting.Value = pair.Value; setting.LastUpdated = DateTime.UtcNow; }
        }
        audit.Add("AdminSystemConfigurationUpdated", "SystemSetting", null,
            new { keys = request.Settings.Keys.Where(Allowed.Contains).ToArray() });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
