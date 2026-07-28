using System.Security.Claims;
using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Admin.ControlPlane;
using CorePortfolio.API.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CorePortfolio.API.Features.Auth.TwoFactor;

public sealed class PrivilegedMfaRequirement : IAuthorizationRequirement;

public sealed class PrivilegedMfaAuthorizationHandler(
    IOptions<TwoFactorOptions> options)
    : AuthorizationHandler<PrivilegedMfaRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PrivilegedMfaRequirement requirement)
    {
        if (!options.Value.EnforceForPrivilegedRoles ||
            HasVerifiedSecondFactor(context.User))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    internal static bool HasVerifiedSecondFactor(ClaimsPrincipal principal) =>
        principal.FindAll("amr").Any(claim =>
            claim.Value is "otp" or "recovery");
}

public sealed class PrivilegedMfaAdminBehavior<TRequest, TResponse>(
    IOptions<TwoFactorOptions> options,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IAdminPermissionRequest &&
            options.Value.EnforceForPrivilegedRoles &&
            AdminPermissionCatalog.Has(
                currentUser.Role,
                AdminPermissionCatalog.AdminAccess) &&
            !PrivilegedMfaAuthorizationHandler.HasVerifiedSecondFactor(
                httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal()))
        {
            throw new ForbiddenAccessException(
                "Two-factor authentication is required for privileged operations.");
        }

        return next(cancellationToken);
    }
}
