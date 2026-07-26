using MediatR;
using Microsoft.AspNetCore.Mvc;
using CorePortfolio.API.Features.Admin.Settings.GetNavigationSettings;
using CorePortfolio.API.Features.Admin.Settings.GetSetting;
using CorePortfolio.API.Features.Admin.Settings.UpdateSetting;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.Admin.Settings;

public record UpdateSettingRequest(string Value);

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        // Public group (Authenticated users)
        var publicGroup = app.MapGroup("/api/settings")
            .WithTags("Settings");

        publicGroup.MapGet("/navigation/features", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var features = await mediator.Send(new GetNavigationSettingsQuery(), cancellationToken);
            return Results.Ok(features);
        }).WithName("GetNavigationSettings");

        publicGroup.MapGet("/{key}", async (string key, IMediator mediator) =>
        {
            var value = await mediator.Send(new GetSettingQuery(key));
            return value != null ? Results.Ok(new { key, value }) : Results.NotFound();
        }).WithName("GetSetting");

        // Admin group
        var adminGroup = app.MapGroup("/api/admin/settings")
            .WithTags("Admin Settings")
            .RequireAuthorization(AdminPermissionCatalog.SettingsManage);

        adminGroup.MapPut("/{key}", async (string key, [FromBody] UpdateSettingRequest request, IMediator mediator) =>
        {
            var success = await mediator.Send(new UpdateSettingCommand(key, request.Value));
            return success ? Results.NoContent() : Results.BadRequest();
        }).WithName("UpdateSetting");
    }
}
