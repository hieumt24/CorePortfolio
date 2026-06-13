using MediatR;

namespace CorePortfolio.API.Features.Transactions.GetAssetTransactions;

public static class GetAssetTransactionsEndpoint
{
    public static void MapGetAssetTransactionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assets/{assetId}/transactions", async (Guid assetId, ISender sender) =>
        {
            var query = new GetAssetTransactionsQuery(assetId);
            var transactions = await sender.Send(query);
            
            return Results.Ok(transactions);
        })
        .WithName("GetAssetTransactions")
        .WithTags("Transactions")
        .Produces<List<TransactionDto>>();
    }
}
