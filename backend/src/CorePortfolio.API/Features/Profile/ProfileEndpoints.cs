using CorePortfolio.API.Features.Profile.ChangePassword;
using CorePortfolio.API.Features.Profile.GetProfile;
using CorePortfolio.API.Features.Profile.UpdateProfile;
using CorePortfolio.API.Features.Auth.TwoFactor;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Profile;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetProfileQuery(), cancellationToken)));

        group.MapPut("/", async (
            ISender sender,
            UpdateProfileRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateProfileCommand(request.Username, request.DisplayName, request.Email),
                cancellationToken);
            return Results.Ok(result);
        });

        group.MapPut("/password", async (
            ISender sender,
            ChangePasswordRequest request,
            CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new ChangePasswordCommand(
                    request.CurrentPassword,
                    request.NewPassword,
                    request.ConfirmPassword),
                cancellationToken);
            return Results.NoContent();
        });

        group.MapGet("/2fa", async (
            ISender sender,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new GetTwoFactorStatusQuery(),
                cancellationToken)));

        group.MapPost("/2fa/setup", async (
            ISender sender,
            BeginProfileTwoFactorSetupRequest request,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new BeginProfileTwoFactorSetupCommand(request.CurrentPassword),
                cancellationToken)));

        group.MapPost("/2fa/recovery-codes", async (
            ISender sender,
            VerifyProfileTwoFactorRequest request,
            CancellationToken cancellationToken) =>
            Results.Ok(new
            {
                recoveryCodes = await sender.Send(
                    new RegenerateRecoveryCodesCommand(
                        request.CurrentPassword,
                        request.Code),
                    cancellationToken)
            }));

        group.MapDelete("/2fa", async (
            ISender sender,
            [FromBody] VerifyProfileTwoFactorRequest request,
            CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new DisableTwoFactorCommand(
                    request.CurrentPassword,
                    request.Code),
                cancellationToken);
            return Results.NoContent();
        });
    }
}

public sealed record BeginProfileTwoFactorSetupRequest(string CurrentPassword);

public sealed record VerifyProfileTwoFactorRequest(
    string CurrentPassword,
    string Code);
