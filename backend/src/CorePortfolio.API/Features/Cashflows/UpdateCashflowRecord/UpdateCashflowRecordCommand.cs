using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.UpdateCashflowRecord;

public record UpdateCashflowRecordCommand(Guid Id, Guid PortfolioId, Guid CategoryId, decimal Amount, string Currency, DateTime Date, string Description) : IRequest;

public class UpdateCashflowRecordHandler : IRequestHandler<UpdateCashflowRecordCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCashflowRecordHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateCashflowRecordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId.Value;

        var cashflow = await _dbContext.CashflowRecords
            .Include(c => c.Transaction)
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == userId, cancellationToken);

        if (cashflow == null)
            throw new Exception("Cashflow not found.");

        var category = await _dbContext.CashflowCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && (c.IsGlobal || c.UserId == userId), cancellationToken);

        if (category == null)
            throw new Exception("Category not found.");

        cashflow.PortfolioId = request.PortfolioId;
        cashflow.CategoryId = request.CategoryId;
        cashflow.Amount = request.Amount;
        cashflow.Currency = request.Currency;
        cashflow.Date = request.Date;
        cashflow.Description = request.Description;

        // If there's an associated Transaction, update it too
        if (cashflow.Transaction != null)
        {
            cashflow.Transaction.PortfolioId = request.PortfolioId;
            cashflow.Transaction.Quantity = request.Amount;
            cashflow.Transaction.Date = request.Date;
            cashflow.Transaction.Type = category.Type == CashflowType.Income ? TransactionType.Deposit : TransactionType.Withdrawal;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
