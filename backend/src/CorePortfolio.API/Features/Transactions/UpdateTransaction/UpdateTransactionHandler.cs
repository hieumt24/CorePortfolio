using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Common;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Transactions.UpdateTransaction;

public class UpdateTransactionHandler : IRequestHandler<UpdateTransactionCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly TransactionLedgerService _ledgerService;

    public UpdateTransactionHandler(AppDbContext dbContext, ICurrentUserService currentUserService, TransactionLedgerService ledgerService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _ledgerService = ledgerService;
    }

    public async Task Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .Include(t => t.Asset)
                .ThenInclude(a => a!.Portfolio)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId &&
                t.Asset!.Portfolio!.UserId == _currentUserService.UserId, cancellationToken);

        if (transaction == null)
            throw new ResourceNotFoundException("Không tìm thấy giao dịch.");


        transaction.Type = request.Type;
        transaction.Quantity = request.Quantity;
        transaction.Price = request.Price;
        transaction.Fee = request.Fee;
        transaction.Notes = request.Notes?.Trim() ?? string.Empty;
        
        if (request.Timestamp.HasValue)
        {
            transaction.Date = request.Timestamp.Value;
        }

        await _ledgerService.ValidateHoldingAsync(transaction, _currentUserService.UserId!.Value, cancellationToken);
        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        _dbContext.Transactions.Update(transaction);
        await _ledgerService.SyncLedgerEntryAsync(transaction, cancellationToken, request.Currency);
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
    }
}
