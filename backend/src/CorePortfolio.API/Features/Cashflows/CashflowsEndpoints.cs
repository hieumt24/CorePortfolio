using CorePortfolio.API.Features.Cashflows.CreateCashflowCategory;
using CorePortfolio.API.Features.Cashflows.CreateCashflowRecord;
using CorePortfolio.API.Features.Cashflows.GetCashflowCategories;
using CorePortfolio.API.Features.Cashflows.GetCashflows;
using CorePortfolio.API.Features.Cashflows.GetCashflowSummary;
using CorePortfolio.API.Features.Cashflows.UpdateCashflowCategory;
using CorePortfolio.API.Features.Cashflows.DeleteCashflowCategory;
using CorePortfolio.API.Features.Cashflows.UpdateCashflowRecord;
using CorePortfolio.API.Features.Cashflows.DeleteCashflowRecord;
using CorePortfolio.API.Features.Cashflows.GetDailyCashflowSummary;
using CorePortfolio.API.Features.Cashflows.GetMonthlyCashflowReport;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Cashflows;

public static class CashflowsEndpoints
{
    public static void MapCashflowsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cashflows")
            .WithTags("Cashflows")
            .RequireAuthorization();

        // Categories
        group.MapGet("/categories", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCashflowCategoriesQuery());
            return Results.Ok(result);
        });

        group.MapPost("/categories", async ([FromBody] CreateCashflowCategoryCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapPut("/categories/{id}", async (Guid id, [FromBody] UpdateCashflowCategoryCommand command, IMediator mediator) =>
        {
            if (id != command.Id)
                return Results.BadRequest("ID mismatch");
                
            await mediator.Send(command);
            return Results.NoContent();
        });

        group.MapDelete("/categories/{id}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteCashflowCategoryCommand(id));
            return Results.NoContent();
        });

        // Cashflows
        group.MapGet("/", async ([AsParameters] GetCashflowsQuery query, IMediator mediator) =>
        {
            var result = await mediator.Send(query);
            return Results.Ok(result);
        });

        group.MapPost("/", async ([FromBody] CreateCashflowRecordCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Ok(result);
        });

        group.MapPut("/{id}", async (Guid id, [FromBody] UpdateCashflowRecordCommand command, IMediator mediator) =>
        {
            if (id != command.Id)
                return Results.BadRequest("ID mismatch");
                
            await mediator.Send(command);
            return Results.NoContent();
        });

        group.MapDelete("/{id}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteCashflowRecordCommand(id));
            return Results.NoContent();
        });

        group.MapGet("/summary", async ([AsParameters] GetCashflowSummaryQuery query, IMediator mediator) =>
        {
            var result = await mediator.Send(query);
            return Results.Ok(result);
        });

        group.MapGet("/summary/daily", async ([AsParameters] GetDailyCashflowSummaryQuery query, IMediator mediator) =>
        {
            var result = await mediator.Send(query);
            return Results.Ok(result);
        });

        group.MapGet("/report/monthly", async ([AsParameters] GetMonthlyCashflowReportQuery query, IMediator mediator) =>
        {
            var result = await mediator.Send(query);
            return Results.Ok(result);
        });
    }
}
