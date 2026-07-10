using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using CorePortfolio.API.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Budgets;

public record SetBudgetCommand(Guid CategoryId, decimal MonthlyLimit) : IRequest<Guid>;

public class SetBudgetHandler : IRequestHandler<SetBudgetCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public SetBudgetHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(SetBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var budget = await _dbContext.Budgets
            .FirstOrDefaultAsync(b => b.UserId == userId && b.CategoryId == request.CategoryId, cancellationToken);

        if (budget != null)
        {
            budget.MonthlyLimit = request.MonthlyLimit;
        }
        else
        {
            budget = new Budget
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CategoryId = request.CategoryId,
                MonthlyLimit = request.MonthlyLimit
            };
            _dbContext.Budgets.Add(budget);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return budget.Id;
    }
}
