using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Auth;
using CorePortfolio.API.Features.Auth.TwoFactor;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CorePortfolio.API.Features.Admin.ControlPlane;

public sealed record GetTwoFactorCoverageQuery
    : IRequest<object>, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.UsersRead;
}

public sealed class GetTwoFactorCoverageHandler(
    AppDbContext dbContext,
    IOptions<TwoFactorOptions> options)
    : IRequestHandler<GetTwoFactorCoverageQuery, object>
{
    public async Task<object> Handle(
        GetTwoFactorCoverageQuery request,
        CancellationToken cancellationToken)
    {
        var privilegedRoles = AdminPermissionCatalog.Roles
            .Where(role => AdminPermissionCatalog.Has(
                role,
                AdminPermissionCatalog.AdminAccess))
            .ToArray();
        var privilegedUsers = dbContext.Users.AsNoTracking()
            .Where(user => user.IsActive && privilegedRoles.Contains(user.Role));
        var total = await privilegedUsers.CountAsync(cancellationToken);
        var enrolled = await privilegedUsers.CountAsync(
            user => user.TwoFactorEnabled,
            cancellationToken);

        return new
        {
            privilegedAccounts = total,
            enrolledAccounts = enrolled,
            pendingAccounts = total - enrolled,
            enrollmentPercentage = total == 0
                ? 100
                : Math.Round(enrolled * 100m / total, 1),
            enforcementEnabled = options.Value.EnforceForPrivilegedRoles,
            readyForEnforcement = total > 0 && enrolled == total
        };
    }
}

public sealed record ResetUserTwoFactorCommand(
    Guid UserId,
    string Confirmation,
    string Reason)
    : IRequest<bool>, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.TwoFactorReset;
}

public sealed class ResetUserTwoFactorHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUser,
    AuthSessionService authSessionService,
    AuditWriter auditWriter)
    : IRequestHandler<ResetUserTwoFactorCommand, bool>
{
    public async Task<bool> Handle(
        ResetUserTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        if (!AdminPermissionCatalog.Has(
                currentUser.Role,
                AdminPermissionCatalog.TwoFactorReset))
            throw new ForbiddenAccessException();
        if (currentUser.UserId == request.UserId)
            throw new ResourceConflictException(
                "Use a separate SuperAdmin account for two-factor recovery.");

        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == request.UserId,
            cancellationToken);
        if (user is null) return false;
        if (!string.Equals(
                request.Confirmation.Trim(),
                user.Username,
                StringComparison.Ordinal))
            throw new RequestValidationException(
                "Enter the target username exactly to confirm the reset.");

        var reason = request.Reason.Trim();
        if (reason.Length is < 10 or > 200)
            throw new RequestValidationException(
                "A recovery reason between 10 and 200 characters is required.");

        var wasEnabled = user.TwoFactorEnabled;
        user.TwoFactorEnabled = false;
        user.TwoFactorSecretEncrypted = null;
        user.TwoFactorEnabledAt = null;
        user.LastAcceptedTotpTimeStep = null;

        dbContext.TwoFactorRecoveryCodes.RemoveRange(
            await dbContext.TwoFactorRecoveryCodes
                .Where(item => item.UserId == user.Id)
                .ToListAsync(cancellationToken));
        dbContext.TwoFactorChallenges.RemoveRange(
            await dbContext.TwoFactorChallenges
                .Where(item => item.UserId == user.Id)
                .ToListAsync(cancellationToken));
        var revokedSessions = await authSessionService.RevokeAllForUserAsync(
            user.Id,
            "Two-factor authentication reset by SuperAdmin",
            cancellationToken);
        auditWriter.Add(
            "UserTwoFactorReset",
            "User",
            user.Id.ToString(),
            new
            {
                wasEnabled,
                revokedSessions,
                reason
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
