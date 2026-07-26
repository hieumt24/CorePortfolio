using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Profile.GetProfile;

public sealed record GetProfileQuery : IRequest<ProfileResponse>;

public sealed class GetProfileHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    public async Task<ProfileResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required.");

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new ProfileResponse(
                user.Id,
                user.Username,
                user.DisplayName ?? user.Username,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.LastLoginAt))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("Profile was not found.");
    }
}
