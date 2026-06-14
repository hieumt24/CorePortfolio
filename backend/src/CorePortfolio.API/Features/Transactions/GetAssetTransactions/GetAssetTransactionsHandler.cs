using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Transactions.GetAssetTransactions;

public class GetAssetTransactionsHandler : IRequestHandler<GetAssetTransactionsQuery, List<TransactionDto>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAssetTransactionsHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<TransactionDto>> Handle(GetAssetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _context.Transactions
            .Where(t => t.AssetId == request.AssetId && t.Portfolio != null && t.Portfolio.UserId == _currentUserService.UserId)
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
