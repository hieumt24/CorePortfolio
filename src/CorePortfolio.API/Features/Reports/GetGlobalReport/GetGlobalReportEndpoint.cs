using MediatR;

namespace CorePortfolio.API.Features.Reports.GetGlobalReport;

public static class GetGlobalReportEndpoint
{
    public static void MapGetGlobalReportEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/global", async (ISender sender, CorePortfolio.API.Services.ICurrentUserService currentUserService) =>
        {
            if (currentUserService.UserId == null) return Results.Unauthorized();
            var result = await sender.Send(new GetGlobalReportQuery(currentUserService.UserId.Value));
            return Results.Ok(result);
        })
        .WithName("GetGlobalReport")
        .WithTags("Reports")
        .Produces<GlobalReportDto>();
    }
}
