using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.Domain.Entities;
using CorePortfolio.API.Features.Auth;

namespace CorePortfolio.API.Features.Profile.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest;

public sealed class ChangePasswordHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    AuthSessionService authSessionService) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required.");

        if (request.NewPassword != request.ConfirmPassword)
            throw new RequestValidationException("Password confirmation does not match.");
        if (request.NewPassword.Length is < 8 or > 72)
            throw new RequestValidationException("New password must contain between 8 and 72 characters.");
        if (request.CurrentPassword == request.NewPassword)
            throw new RequestValidationException("New password must be different from the current password.");

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Profile was not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await authSessionService.RevokeAllForUserAsync(
            userId,
            "Password changed",
            cancellationToken);
        dbContext.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = userId,
            Action = "UserPasswordChanged",
            EntityType = "User",
            EntityId = userId.ToString(),
            Outcome = "Succeeded",
            OccurredAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
