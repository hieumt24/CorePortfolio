using MediatR;

namespace CorePortfolio.API.Features.Transactions.DeleteAllTransactions;

public static class DeleteAllTransactionsEndpoint
{
    public static void MapDeleteAllTransactionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/transactions", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new DeleteAllTransactionsCommand(),
                cancellationToken);

            return Results.Ok(result);
        })
        .WithName("DeleteAllTransactions")
        .WithTags("Transactions")
        .RequireAuthorization();
    }
}
