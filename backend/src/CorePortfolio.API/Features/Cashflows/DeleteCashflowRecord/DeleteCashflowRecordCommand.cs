using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.DeleteCashflowRecord;

public record DeleteCashflowRecordCommand(Guid Id) : IRequest;

public class DeleteCashflowRecordHandler : IRequestHandler<DeleteCashflowRecordCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCashflowRecordHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteCashflowRecordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId.Value;

        var cashflow = await _dbContext.CashflowRecords
            .Include(c => c.Transaction)
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == userId, cancellationToken);

        if (cashflow != null)
            {
            // Delete associated transaction if it exists
            if (cashflow.Transaction != null)
            {
                _dbContext.Transactions.Remove(cashflow.Transaction);
            }

            _dbContext.CashflowRecords.Remove(cashflow);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
