using MediatR;
using Microsoft.AspNetCore.Mvc;
using CorePortfolio.API.Features.Admin.Settings.GetSetting;
using CorePortfolio.API.Features.Admin.Settings.UpdateSetting;

namespace CorePortfolio.API.Features.Admin.Settings;

public record UpdateSettingRequest(string Value);

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/settings/{key}", async (string key, IMediator mediator) =>
        {
            var value = await mediator.Send(new GetSettingQuery(key));
            return value != null ? Results.Ok(new { key, value }) : Results.NotFound();
        })
        .WithName("GetSetting")
        .WithTags("Settings");

        app.MapPut("/api/settings/{key}", async (string key, [FromBody] UpdateSettingRequest request, IMediator mediator) =>
        {
            var success = await mediator.Send(new UpdateSettingCommand(key, request.Value));
            return success ? Results.NoContent() : Results.BadRequest();
        })
        .WithName("UpdateSetting")
        .WithTags("Settings");
    }
}
