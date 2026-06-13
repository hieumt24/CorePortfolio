using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Transactions.GetAssetTransactions;

public class GetAssetTransactionsHandler : IRequestHandler<GetAssetTransactionsQuery, List<TransactionDto>>
{
    private readonly AppDbContext _context;

    public GetAssetTransactionsHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TransactionDto>> Handle(GetAssetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _context.Transactions
            .Where(t => t.AssetId == request.AssetId)
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionDto(
                t.Id,
                (int)t.Type,
                t.Quantity,
                t.Price,
                t.Date
            ))
            .ToListAsync(cancellationToken);

        return transactions;
    }
}
