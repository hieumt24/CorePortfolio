using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Transactions.UpdateTransaction;

public class UpdateTransactionHandler : IRequestHandler<UpdateTransactionCommand>
{
    private readonly AppDbContext _dbContext;

    public UpdateTransactionHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (transaction == null)
            throw new Exception("Transaction not found");

        transaction.Type = request.Type;
        transaction.Quantity = request.Quantity;
        transaction.Price = request.Price;
        
        if (request.Timestamp.HasValue)
        {
            transaction.Date = request.Timestamp.Value;
        }


        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
