using CorePortfolio.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Transactions.CreateTransaction;

public record CreateTransactionRequest(Guid PortfolioId, Guid AssetId, TransactionType Type, decimal Quantity,
    decimal Price, string? Currency, DateTime? Timestamp, decimal Fee = 0, string? Notes = null);

public static class CreateTransactionEndpoint
{
    public static void MapCreateTransactionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transactions", async ([FromBody] CreateTransactionRequest request, IMediator mediator) =>
        {
            var command = new CreateTransactionCommand(request.PortfolioId, request.AssetId, request.Type,
                request.Quantity, request.Price, request.Currency, request.Timestamp, request.Fee, request.Notes);
            var result = await mediator.Send(command);
            return Results.Created($"/api/transactions/{result.Id}", result);
        })
        .WithName("CreateTransaction")
        .WithTags("Transactions");
    }
}
