using CorePortfolio.API.Features.Dashboard.GetFinancialHealth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Dashboard;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard/financial-health", async (IMediator mediator, [FromQuery] string currency = "VND") =>
        {
            var result = await mediator.Send(new GetFinancialHealthQuery(currency));
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
