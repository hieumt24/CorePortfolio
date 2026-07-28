using System.Text.Json;
using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Auth.Login;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CorePortfolio.API.Features.Auth.TwoFactor;

public sealed record TwoFactorSetupResponse(
    string ChallengeToken,
    string ProvisioningUri,
    string ManualKey,
    DateTime ExpiresAt);

public sealed record TwoFactorStatusResponse(
    bool IsAvailable,
    bool IsEnabled,
    bool IsRequired,
    bool IsPrivilegedRole,
    DateTime? EnabledAt,
    int RecoveryCodesRemaining);

public sealed record BeginLoginTwoFactorSetupCommand(
    string ChallengeToken) : IRequest<TwoFactorSetupResponse?>;

public sealed class BeginLoginTwoFactorSetupHandler(
    AppDbContext dbContext,
    TwoFactorChallengeService challengeService,
    TwoFactorSecretProtector secretProtector,
    TotpService totpService)
    : IRequestHandler<BeginLoginTwoFactorSetupCommand, TwoFactorSetupResponse?>
{
    public async Task<TwoFactorSetupResponse?> Handle(
        BeginLoginTwoFactorSetupCommand request,
        CancellationToken cancellationToken)
    {
        secretProtector.EnsureConfigured();
        var now = DateTime.UtcNow;
        var challenge = await challengeService.FindActiveAsync(
            request.ChallengeToken,
            TwoFactorChallengePurpose.Enrollment,
            now,
            cancellationToken);
        if (challenge is null || challenge.User.TwoFactorEnabled) return null;

        string secret;
        if (string.IsNullOrWhiteSpace(challenge.PendingSecretEncrypted))
        {
            secret = totpService.GenerateSecret();
            challenge.PendingSecretEncrypted = secretProtector.Protect(secret, challenge.UserId);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            secret = secretProtector.Unprotect(
                challenge.PendingSecretEncrypted,
                challenge.UserId);
        }

        return new TwoFactorSetupResponse(
            request.ChallengeToken,
            totpService.BuildProvisioningUri(challenge.User.Username, secret),
            secret,
            challenge.ExpiresAt);
    }
}

public sealed record VerifyTwoFactorCommand(
    string ChallengeToken,
    string? Code,
    string? RecoveryCode) : IRequest<AuthSessionResult?>;

public sealed class VerifyTwoFactorHandler(
    AppDbContext dbContext,
    TwoFactorChallengeService challengeService,
    TwoFactorSecretProtector secretProtector,
    TotpService totpService,
    RecoveryCodeService recoveryCodeService,
    AuthSessionService authSessionService,
    AuthLoginCompletionService loginCompletionService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<VerifyTwoFactorCommand, AuthSessionResult?>
{
    public async Task<AuthSessionResult?> Handle(
        VerifyTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var challenge = await challengeService.FindActiveAsync(
            request.ChallengeToken,
            null,
            now,
            cancellationToken);
        if (challenge is null) return null;

        var user = challenge.User;
        var authenticationMethod = "otp";
        var accepted = false;
        long acceptedTimeStep = 0;
        TwoFactorRecoveryCode? usedRecoveryCode = null;

        var encryptedSecret = challenge.Purpose == TwoFactorChallengePurpose.Enrollment
            ? challenge.PendingSecretEncrypted
            : user.TwoFactorSecretEncrypted;
        if (!string.IsNullOrWhiteSpace(encryptedSecret))
        {
            var secret = secretProtector.Unprotect(encryptedSecret, user.Id);
            accepted = totpService.TryVerify(
                secret,
                request.Code,
                now,
                out acceptedTimeStep);
            if (accepted && user.LastAcceptedTotpTimeStep >= acceptedTimeStep)
                accepted = false;
        }

        if (!accepted &&
            challenge.Purpose == TwoFactorChallengePurpose.Login &&
            !string.IsNullOrWhiteSpace(request.RecoveryCode))
        {
            var recoveryHash = RecoveryCodeService.Hash(request.RecoveryCode);
            usedRecoveryCode = await dbContext.TwoFactorRecoveryCodes
                .SingleOrDefaultAsync(
                    item => item.UserId == user.Id &&
                        item.CodeHash == recoveryHash &&
                        item.UsedAt == null,
                    cancellationToken);
            accepted = usedRecoveryCode is not null;
            authenticationMethod = "recovery";
        }

        if (!accepted)
        {
            challenge.FailedAttemptCount++;
            AddAudit(
                dbContext,
                user.Id,
                "TwoFactorVerificationFailed",
                "Failed",
                httpContextAccessor,
                new
                {
                    purpose = challenge.Purpose.ToString(),
                    attempts = challenge.FailedAttemptCount
                },
                now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        var factorClaimed = authenticationMethod == "otp"
            ? await dbContext.Users
                .Where(item =>
                    item.Id == user.Id &&
                    (!item.LastAcceptedTotpTimeStep.HasValue ||
                        item.LastAcceptedTotpTimeStep < acceptedTimeStep))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        item => item.LastAcceptedTotpTimeStep,
                        acceptedTimeStep),
                    cancellationToken) == 1
            : await dbContext.TwoFactorRecoveryCodes
                .Where(item =>
                    item.Id == usedRecoveryCode!.Id &&
                    item.UsedAt == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(item => item.UsedAt, now),
                    cancellationToken) == 1;
        if (!factorClaimed) return null;

        var challengeClaimed = await dbContext.TwoFactorChallenges
            .Where(item =>
                item.Id == challenge.Id &&
                item.ConsumedAt == null &&
                item.ExpiresAt > now &&
                item.FailedAttemptCount < item.MaxAttempts)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.ConsumedAt, now),
                cancellationToken);
        if (challengeClaimed != 1) return null;

        IReadOnlyList<string>? recoveryCodes = null;
        if (challenge.Purpose == TwoFactorChallengePurpose.Enrollment)
        {
            if (string.IsNullOrWhiteSpace(challenge.PendingSecretEncrypted))
                return null;

            user.TwoFactorSecretEncrypted = challenge.PendingSecretEncrypted;
            user.TwoFactorEnabled = true;
            user.TwoFactorEnabledAt = now;
            await authSessionService.RevokeAllForUserAsync(
                user.Id,
                "Two-factor authentication enabled",
                cancellationToken);
            dbContext.TwoFactorRecoveryCodes.RemoveRange(
                await dbContext.TwoFactorRecoveryCodes
                    .Where(item => item.UserId == user.Id)
                    .ToListAsync(cancellationToken));
            recoveryCodes = recoveryCodeService.GenerateCodes();
            dbContext.TwoFactorRecoveryCodes.AddRange(recoveryCodes.Select(code =>
                new TwoFactorRecoveryCode
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CodeHash = RecoveryCodeService.Hash(code),
                    CreatedAt = now
                }));
            AddAudit(
                dbContext,
                user.Id,
                "TwoFactorEnabled",
                "Succeeded",
                httpContextAccessor,
                null,
                now);
        }
        else
        {
            AddAudit(
                dbContext,
                user.Id,
                "TwoFactorVerified",
                "Succeeded",
                httpContextAccessor,
                new { authenticationMethod },
                now);
        }

        var session = await loginCompletionService.CompleteAsync(
            user,
            now,
            authenticationMethod,
            now,
            cancellationToken);
        session.Response.RecoveryCodes = recoveryCodes;
        return session;
    }

    internal static void AddAudit(
        AppDbContext dbContext,
        Guid userId,
        string action,
        string outcome,
        IHttpContextAccessor httpContextAccessor,
        object? metadata,
        DateTime now)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = userId,
            Action = action,
            EntityType = "User",
            EntityId = userId.ToString(),
            Outcome = outcome,
            IpAddress = ClientIpAddress.Resolve(httpContextAccessor.HttpContext),
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
            OccurredAt = now
        });
    }
}

public sealed record BeginProfileTwoFactorSetupCommand(
    string CurrentPassword) : IRequest<TwoFactorSetupResponse>;

public sealed class BeginProfileTwoFactorSetupHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    TwoFactorChallengeService challengeService,
    TwoFactorSecretProtector secretProtector,
    TotpService totpService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<BeginProfileTwoFactorSetupCommand, TwoFactorSetupResponse>
{
    public async Task<TwoFactorSetupResponse> Handle(
        BeginProfileTwoFactorSetupCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await dbContext.Users.SingleAsync(
            item => item.Id == userId && item.IsActive,
            cancellationToken);
        if (user.TwoFactorEnabled)
            throw new ResourceConflictException("Two-factor authentication is already enabled.");
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new RequestValidationException("Unable to verify the current credentials.");
        secretProtector.EnsureConfigured();

        var now = DateTime.UtcNow;
        var issued = challengeService.Issue(user, TwoFactorChallengePurpose.Enrollment, now);
        var secret = totpService.GenerateSecret();
        issued.Challenge.PendingSecretEncrypted = secretProtector.Protect(secret, user.Id);
        VerifyTwoFactorHandler.AddAudit(
            dbContext,
            user.Id,
            "TwoFactorEnrollmentStarted",
            "Pending",
            httpContextAccessor,
            null,
            now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TwoFactorSetupResponse(
            issued.Token,
            totpService.BuildProvisioningUri(user.Username, secret),
            secret,
            issued.Challenge.ExpiresAt);
    }
}

public sealed record GetTwoFactorStatusQuery : IRequest<TwoFactorStatusResponse>;

public sealed class GetTwoFactorStatusHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    TwoFactorPolicy policy,
    TwoFactorSecretProtector secretProtector)
    : IRequestHandler<GetTwoFactorStatusQuery, TwoFactorStatusResponse>
{
    public async Task<TwoFactorStatusResponse> Handle(
        GetTwoFactorStatusQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await dbContext.Users.AsNoTracking().SingleAsync(
            item => item.Id == userId,
            cancellationToken);
        var recoveryCodesRemaining = await dbContext.TwoFactorRecoveryCodes.CountAsync(
            item => item.UserId == userId && item.UsedAt == null,
            cancellationToken);
        return new TwoFactorStatusResponse(
            secretProtector.IsConfigured,
            user.TwoFactorEnabled,
            policy.RequiresTwoFactor(user),
            policy.IsPrivilegedRole(user.Role),
            user.TwoFactorEnabledAt,
            recoveryCodesRemaining);
    }
}

public sealed record RegenerateRecoveryCodesCommand(
    string CurrentPassword,
    string Code) : IRequest<IReadOnlyList<string>>;

public sealed class RegenerateRecoveryCodesHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    TwoFactorSecretProtector secretProtector,
    TotpService totpService,
    RecoveryCodeService recoveryCodeService,
    AuthSessionService authSessionService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<RegenerateRecoveryCodesCommand, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(
        RegenerateRecoveryCodesCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await dbContext.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        var now = DateTime.UtcNow;
        ValidatePasswordAndTotp(
            user,
            request.CurrentPassword,
            request.Code,
            now,
            secretProtector,
            totpService,
            out var timeStep);
        user.LastAcceptedTotpTimeStep = timeStep;

        dbContext.TwoFactorRecoveryCodes.RemoveRange(
            await dbContext.TwoFactorRecoveryCodes
                .Where(item => item.UserId == userId)
                .ToListAsync(cancellationToken));
        var codes = recoveryCodeService.GenerateCodes();
        dbContext.TwoFactorRecoveryCodes.AddRange(codes.Select(code =>
            new TwoFactorRecoveryCode
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CodeHash = RecoveryCodeService.Hash(code),
                CreatedAt = now
            }));
        var retainedTokenId = httpContextAccessor.HttpContext?.User
            .FindFirstValue(JwtRegisteredClaimNames.Jti);
        await authSessionService.RevokeAllForUserExceptAsync(
            userId,
            retainedTokenId,
            "Two-factor recovery codes regenerated",
            cancellationToken);
        VerifyTwoFactorHandler.AddAudit(
            dbContext,
            userId,
            "TwoFactorRecoveryCodesRegenerated",
            "Succeeded",
            httpContextAccessor,
            null,
            now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return codes;
    }

    internal static void ValidatePasswordAndTotp(
        User user,
        string currentPassword,
        string code,
        DateTime now,
        TwoFactorSecretProtector secretProtector,
        TotpService totpService,
        out long timeStep)
    {
        timeStep = 0;
        if (!user.TwoFactorEnabled ||
            string.IsNullOrWhiteSpace(user.TwoFactorSecretEncrypted) ||
            !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new RequestValidationException("Unable to verify the current credentials.");

        var secret = secretProtector.Unprotect(user.TwoFactorSecretEncrypted, user.Id);
        if (!totpService.TryVerify(secret, code, now, out timeStep) ||
            user.LastAcceptedTotpTimeStep >= timeStep)
            throw new RequestValidationException("Unable to verify the current credentials.");
    }
}

public sealed record DisableTwoFactorCommand(
    string CurrentPassword,
    string Code) : IRequest;

public sealed class DisableTwoFactorHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    TwoFactorPolicy policy,
    TwoFactorSecretProtector secretProtector,
    TotpService totpService,
    AuthSessionService authSessionService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<DisableTwoFactorCommand>
{
    public async Task Handle(
        DisableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await dbContext.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        if (!policy.CanDisable(user))
            throw new ForbiddenAccessException(
                "Two-factor authentication is mandatory for privileged accounts.");

        var now = DateTime.UtcNow;
        RegenerateRecoveryCodesHandler.ValidatePasswordAndTotp(
            user,
            request.CurrentPassword,
            request.Code,
            now,
            secretProtector,
            totpService,
            out _);
        user.TwoFactorEnabled = false;
        user.TwoFactorSecretEncrypted = null;
        user.TwoFactorEnabledAt = null;
        user.LastAcceptedTotpTimeStep = null;
        dbContext.TwoFactorRecoveryCodes.RemoveRange(
            await dbContext.TwoFactorRecoveryCodes
                .Where(item => item.UserId == userId)
                .ToListAsync(cancellationToken));
        dbContext.TwoFactorChallenges.RemoveRange(
            await dbContext.TwoFactorChallenges
                .Where(item => item.UserId == userId && item.ConsumedAt == null)
                .ToListAsync(cancellationToken));
        await authSessionService.RevokeAllForUserAsync(
            userId,
            "Two-factor authentication disabled",
            cancellationToken);
        VerifyTwoFactorHandler.AddAudit(
            dbContext,
            userId,
            "TwoFactorDisabled",
            "Succeeded",
            httpContextAccessor,
            null,
            now);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
