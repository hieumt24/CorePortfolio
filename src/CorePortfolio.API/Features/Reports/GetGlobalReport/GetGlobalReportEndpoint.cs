using MediatR;

namespace CorePortfolio.API.Features.Reports.GetGlobalReport;

public static class GetGlobalReportEndpoint
{
    public static void MapGetGlobalReportEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/global", async (ISender sender) =>
        {
            var result = await sender.Send(new GetGlobalReportQuery());
            return Results.Ok(result);
        })
        .WithName("GetGlobalReport")
        .WithTags("Reports")
        .Produces<GlobalReportDto>();
    }
}
