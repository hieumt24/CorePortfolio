using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Admin.Categories;

public record CreateCategoryRequest(string Name, string DefaultCurrency);

public record UpdateCategoryRequest(string Name, string DefaultCurrency);

public static class CategoriesEndpoints
{
    public static void MapCategoriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/categories")
            .WithTags("Admin Categories");

        group.MapPost("/", async ([FromBody] CreateCategoryRequest request, IMediator mediator) =>
        {
            var id = await mediator.Send(new CreateCategoryCommand(request.Name, request.DefaultCurrency));
            return Results.Created($"/api/admin/categories/{id}", new { Id = id });
        }).RequireAuthorization("Admin");

        group.MapPut("/{id}", async (Guid id, [FromBody] UpdateCategoryRequest request, IMediator mediator) =>
        {
            var success = await mediator.Send(new UpdateCategoryCommand(id, request.Name, request.DefaultCurrency));
            return success ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("Admin");

        group.MapDelete("/{id}", async (Guid id, IMediator mediator) =>
        {
            try
            {
                var success = await mediator.Send(new DeleteCategoryCommand(id));
                return success ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).RequireAuthorization("Admin");

        group.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCategoriesQuery());
            return Results.Ok(result);
        });
    }
}
