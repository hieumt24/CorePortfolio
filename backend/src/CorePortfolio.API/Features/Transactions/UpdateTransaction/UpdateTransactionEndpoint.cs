using CorePortfolio.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Transactions.UpdateTransaction;

public record UpdateTransactionRequest(TransactionType Type, decimal Quantity, decimal Price, string? Currency,
    DateTime? Timestamp, decimal Fee = 0, string? Notes = null);

public static class UpdateTransactionEndpoint
{
    public static void MapUpdateTransactionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/transactions/{id}", async (Guid id, [FromBody] UpdateTransactionRequest request, IMediator mediator) =>
        {
            var command = new UpdateTransactionCommand(id, request.Type, request.Quantity, request.Price,
                request.Currency, request.Timestamp, request.Fee, request.Notes);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithName("UpdateTransaction")
        .WithTags("Transactions");
    }
}
