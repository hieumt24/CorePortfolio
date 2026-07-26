using CorePortfolio.API.Services;
using CorePortfolio.API.Common;
using MediatR;

namespace CorePortfolio.API.Features.Admin.ControlPlane;

public interface IAdminPermissionRequest
{
    string RequiredPermission { get; }
}

public sealed class AdminPermissionBehavior<TRequest, TResponse>(ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IAdminPermissionRequest protectedRequest &&
            !AdminPermissionCatalog.Has(currentUser.Role, protectedRequest.RequiredPermission))
        {
            throw new ForbiddenAccessException(
                $"Permission '{protectedRequest.RequiredPermission}' is required.");
        }

        return next(cancellationToken);
    }
}
