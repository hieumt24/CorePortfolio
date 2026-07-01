using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Budgets;

public static class BudgetsEndpoints
{
    public static void MapBudgetsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/budgets").RequireAuthorization();

        group.MapPost("/", async ([FromBody] SetBudgetRequest request, IMediator mediator) =>
        {
            var command = new SetBudgetCommand(request.CategoryId, request.MonthlyLimit);
            var id = await mediator.Send(command);
            return Results.Ok(new { Id = id });
        });

        group.MapGet("/progress", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetBudgetsProgressQuery());
            return Results.Ok(result);
        });
    }
}

public class SetBudgetRequest
{
    public Guid CategoryId { get; set; }
    public decimal MonthlyLimit { get; set; }
}
