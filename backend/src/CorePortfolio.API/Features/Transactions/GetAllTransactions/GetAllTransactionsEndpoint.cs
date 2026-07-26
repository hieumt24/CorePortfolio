using CorePortfolio.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using CorePortfolio.API.Features.Transactions.DeleteAllTransactions;

namespace CorePortfolio.API.Features.Transactions.GetAllTransactions;

public static class GetAllTransactionsEndpoint
{
    public static void MapGetAllTransactionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions", async (
            [FromQuery] Guid? portfolioId,
            [FromQuery] Guid? assetId,
            [FromQuery] TransactionType? type,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? search,
            [FromQuery] TransactionAssetGroup? assetGroup,
            [FromQuery] decimal? minAmount,
            [FromQuery] decimal? maxAmount,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortDirection,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IMediator mediator) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize < 1 ? 20 : pageSize, 1, 200);
            
            var query = new GetAllTransactionsQuery(
                portfolioId,
                assetId,
                type,
                startDate,
                endDate,
                search,
                assetGroup ?? TransactionAssetGroup.All,
                minAmount,
                maxAmount,
                sortBy ?? "date",
                sortDirection ?? "desc",
                page,
                pageSize);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetAllTransactions")
        .WithTags("Transactions");
    }
}
