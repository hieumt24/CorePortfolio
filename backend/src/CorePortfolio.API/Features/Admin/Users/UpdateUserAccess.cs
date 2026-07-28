using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CorePortfolio.API.Features.Admin.ControlPlane;
using CorePortfolio.API.Common;

namespace CorePortfolio.API.Features.Admin.Users;

public record UpdateUserAccessCommand(Guid UserId, string Role, bool IsActive) : IRequest<AdminUserDto?>;

public sealed class UpdateUserAccessHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUser,
    IOptions<UserActivityOptions> activityOptions,
    AuditWriter auditWriter)
    : IRequestHandler<UpdateUserAccessCommand, AdminUserDto?>
{
    public async Task<AdminUserDto?> Handle(UpdateUserAccessCommand request, CancellationToken cancellationToken)
    {
        if (!AdminPermissionCatalog.Roles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Role is not supported.");
        if (!AdminPermissionCatalog.Has(currentUser.Role, AdminPermissionCatalog.UsersManage))
            throw new ForbiddenAccessException();

        var user = await dbContext.Users
            .Include(item => item.Portfolios)
            .ThenInclude(portfolio => portfolio.Transactions)
            .SingleOrDefaultAsync(item => item.Id == request.UserId, cancellationToken);

        if (user is null)
            return null;

        var removesOwnAdminAccess = currentUser.UserId == user.Id &&
            (!request.IsActive || request.Role is not ("Admin" or "SuperAdmin"));
        if (removesOwnAdminAccess)
            throw new InvalidOperationException("You cannot disable or remove your own administrator access.");

        var removesActiveAdmin = user.Role is "Admin" or "SuperAdmin" && user.IsActive &&
            (request.Role is not ("Admin" or "SuperAdmin") || !request.IsActive);
        if (removesActiveAdmin)
        {
            var hasAnotherActiveAdmin = await dbContext.Users.AnyAsync(
                item => item.Id != user.Id && (item.Role == "Admin" || item.Role == "SuperAdmin") && item.IsActive,
                cancellationToken);
            if (!hasAnotherActiveAdmin)
                throw new InvalidOperationException("At least one active administrator is required.");
        }

        var previousRole = user.Role;
        var previousIsActive = user.IsActive;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        if (previousRole != user.Role || previousIsActive != user.IsActive)
        {
            var sessions = await dbContext.UserSessions
                .Where(item => item.UserId == user.Id && item.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var session in sessions)
            {
                session.RevokedAt = DateTime.UtcNow;
                session.RevokedByUserId = currentUser.UserId;
                session.RevokeReason = "Account access changed";
            }
        }
        auditWriter.Add(
            "UserAccessChanged",
            "User",
            user.Id.ToString(),
            new
            {
                PreviousRole = previousRole,
                PreviousIsActive = previousIsActive,
                NewRole = user.Role,
                NewIsActive = user.IsActive
            });
        await dbContext.SaveChangesAsync(cancellationToken);

        var onlineCutoff = UserPresence.GetOnlineCutoff(activityOptions.Value);
        var isOnline = user.IsActive &&
            user.LastActivityAt is not null &&
            user.LastActivityAt >= onlineCutoff;

        return new AdminUserDto(user.Id, user.Username, user.Role, user.IsActive, user.CreatedAt,
            user.LastLoginAt, user.LastLoginIpAddress, user.LastActivityAt, isOnline,
            user.TwoFactorEnabled,
            user.Portfolios.Count,
            user.Portfolios.SelectMany(portfolio => portfolio.Transactions).Count());
    }
}
