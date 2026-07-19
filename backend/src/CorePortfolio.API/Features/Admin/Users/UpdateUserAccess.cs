using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.Users;

public record UpdateUserAccessCommand(Guid UserId, string Role, bool IsActive) : IRequest<AdminUserDto?>;

public sealed class UpdateUserAccessHandler(AppDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<UpdateUserAccessCommand, AdminUserDto?>
{
    public async Task<AdminUserDto?> Handle(UpdateUserAccessCommand request, CancellationToken cancellationToken)
    {
        if (request.Role is not ("Admin" or "User"))
            throw new ArgumentException("Role must be Admin or User.");

        var user = await dbContext.Users
            .Include(item => item.Portfolios)
            .ThenInclude(portfolio => portfolio.Transactions)
            .SingleOrDefaultAsync(item => item.Id == request.UserId, cancellationToken);

        if (user is null)
            return null;

        var removesOwnAdminAccess = currentUser.UserId == user.Id &&
            (!request.IsActive || request.Role != "Admin");
        if (removesOwnAdminAccess)
            throw new InvalidOperationException("You cannot disable or remove your own administrator access.");

        var removesActiveAdmin = user.Role == "Admin" && user.IsActive &&
            (request.Role != "Admin" || !request.IsActive);
        if (removesActiveAdmin)
        {
            var hasAnotherActiveAdmin = await dbContext.Users.AnyAsync(
                item => item.Id != user.Id && item.Role == "Admin" && item.IsActive,
                cancellationToken);
            if (!hasAnotherActiveAdmin)
                throw new InvalidOperationException("At least one active administrator is required.");
        }

        user.Role = request.Role;
        user.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AdminUserDto(user.Id, user.Username, user.Role, user.IsActive, user.CreatedAt,
            user.LastLoginAt, user.Portfolios.Count,
            user.Portfolios.SelectMany(portfolio => portfolio.Transactions).Count());
    }
}
