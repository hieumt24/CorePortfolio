using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Transactions.DeleteTransaction;

public static class DeleteTransactionEndpoint
{
    public static void MapDeleteTransactionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/transactions/{id}", async (Guid id, IMediator mediator) =>
        {
            var command = new DeleteTransactionCommand(id);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithName("DeleteTransaction")
        .WithTags("Transactions");
    }
}
