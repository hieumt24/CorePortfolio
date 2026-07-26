using CorePortfolio.API.Features.Profile.ChangePassword;
using CorePortfolio.API.Features.Profile.GetProfile;
using CorePortfolio.API.Features.Profile.UpdateProfile;
using MediatR;

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
    }
}
