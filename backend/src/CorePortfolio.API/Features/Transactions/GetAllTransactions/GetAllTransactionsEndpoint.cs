using CorePortfolio.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IMediator mediator) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            
            var query = new GetAllTransactionsQuery(portfolioId, assetId, type, startDate, endDate, page, pageSize);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetAllTransactions")
        .WithTags("Transactions");
    }
}
