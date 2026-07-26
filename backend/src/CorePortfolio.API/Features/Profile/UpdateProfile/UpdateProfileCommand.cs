using System.Net.Mail;
using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Profile.UpdateProfile;

public sealed record UpdateProfileCommand(
    string Username,
    string DisplayName,
    string? Email) : IRequest<ProfileResponse>;

public sealed class UpdateProfileHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateProfileCommand, ProfileResponse>
{
    public async Task<ProfileResponse> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required.");
        var username = request.Username.Trim();
        var displayName = request.DisplayName.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email)
            ? null
            : request.Email.Trim().ToLowerInvariant();

        if (username.Length is < 3 or > 50)
            throw new RequestValidationException("Username must contain between 3 and 50 characters.");
        if (displayName.Length is < 2 or > 80)
            throw new RequestValidationException("Display name must contain between 2 and 80 characters.");
        if (email is not null && (email.Length > 160 || !MailAddress.TryCreate(email, out _)))
            throw new RequestValidationException("Email address is not valid.");

        if (await dbContext.Users.AnyAsync(
                user => user.Id != userId && user.Username.ToLower() == username.ToLower(),
                cancellationToken))
            throw new ResourceConflictException("Username already exists.");

        if (email is not null && await dbContext.Users.AnyAsync(
                user => user.Id != userId && user.Email == email,
                cancellationToken))
            throw new ResourceConflictException("Email address is already in use.");

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Profile was not found.");

        user.Username = username;
        user.DisplayName = displayName;
        user.Email = email;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProfileResponse(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Email,
            user.Role,
            user.CreatedAt,
            user.LastLoginAt);
    }
}
