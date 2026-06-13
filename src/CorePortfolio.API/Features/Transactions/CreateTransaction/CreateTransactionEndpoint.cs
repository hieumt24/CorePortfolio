using CorePortfolio.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Transactions.CreateTransaction;

public record CreateTransactionRequest(Guid PortfolioId, Guid AssetId, TransactionType Type, decimal Quantity, decimal Price, string? Currency);

public static class CreateTransactionEndpoint
{
    public static void MapCreateTransactionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transactions", async ([FromBody] CreateTransactionRequest request, IMediator mediator) =>
        {
            var command = new CreateTransactionCommand(request.PortfolioId, request.AssetId, request.Type, request.Quantity, request.Price, request.Currency);
            var id = await mediator.Send(command);
            return Results.Created($"/api/transactions/{id}", new { Id = id });
        })
        .WithName("CreateTransaction")
        .WithTags("Transactions");
    }
}
