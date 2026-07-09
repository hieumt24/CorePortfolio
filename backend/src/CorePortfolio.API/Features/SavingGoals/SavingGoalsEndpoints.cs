using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.SavingGoals;

public static class SavingGoalsEndpoints
{
    public static void MapSavingGoalsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/saving-goals")
            .RequireAuthorization()
            .WithTags("Saving Goals");

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetSavingGoalsQuery())));

        group.MapPost("/", async ([FromBody] SaveSavingGoalRequest request, IMediator mediator) =>
        {
            var id = await mediator.Send(new SaveSavingGoalCommand(null, request));
            return Results.Ok(new { Id = id });
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] SaveSavingGoalRequest request, IMediator mediator) =>
        {
            await mediator.Send(new SaveSavingGoalCommand(id, request));
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteSavingGoalCommand(id));
            return Results.NoContent();
        });
    }
}

public sealed record SaveSavingGoalRequest(
    Guid PortfolioId,
    Guid? CashAccountId,
    Guid CashflowCategoryId,
    string Name,
    string Description,
    decimal TargetAmount,
    string Currency,
    DateTime Deadline,
    bool IsCompleted);

public sealed record SavingGoalDto(
    Guid Id,
    Guid PortfolioId,
    string PortfolioName,
    Guid? CashAccountId,
    Guid CashflowCategoryId,
    string CategoryName,
    string Name,
    string Description,
    decimal TargetAmount,
    string Currency,
    DateTime Deadline,
    DateTime CreatedAt,
    bool IsCompleted,
    decimal CashAccountBalance,
    decimal SavingCashflowAmount,
    decimal CurrentAmount,
    decimal RemainingAmount,
    decimal ProgressPercentage,
    decimal MonthlyRequiredSaving,
    int DaysRemaining);

public sealed record GetSavingGoalsQuery : IRequest<List<SavingGoalDto>>;

public sealed class GetSavingGoalsHandler : IRequestHandler<GetSavingGoalsQuery, List<SavingGoalDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetSavingGoalsHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<SavingGoalDto>> Handle(GetSavingGoalsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var goals = await _dbContext.SavingGoals
            .AsNoTracking()
            .Include(g => g.Portfolio)
            .Include(g => g.CashflowCategory)
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.IsCompleted)
            .ThenBy(g => g.Deadline)
            .ToListAsync(cancellationToken);

        var result = new List<SavingGoalDto>();
        foreach (var goal in goals)
        {
            var categoryIds = await _dbContext.CashflowCategories
                .AsNoTracking()
                .Where(c => c.Id == goal.CashflowCategoryId || c.ParentCategoryId == goal.CashflowCategoryId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var savingCashflowAmount = await _dbContext.CashflowRecords
                .AsNoTracking()
                .Where(r => r.UserId == userId &&
                    r.PortfolioId == goal.PortfolioId &&
                    r.Currency == goal.Currency &&
                    r.Date >= goal.CreatedAt &&
                    categoryIds.Contains(r.CategoryId))
                .SumAsync(r => r.Amount, cancellationToken);

            var cashAccountBalanceQuery = _dbContext.CashLedgerEntries
                .AsNoTracking()
                .Where(e => e.CashAccount.Portfolio.UserId == userId &&
                    e.CashAccount.PortfolioId == goal.PortfolioId &&
                    e.CashAccount.Currency == goal.Currency);

            if (goal.CashAccountId.HasValue)
            {
                cashAccountBalanceQuery = cashAccountBalanceQuery.Where(e => e.CashAccountId == goal.CashAccountId.Value);
            }

            var cashAccountBalance = await cashAccountBalanceQuery.SumAsync(e => e.Amount, cancellationToken);
            result.Add(ToDto(goal, cashAccountBalance, savingCashflowAmount));
        }

        return result;
    }

    private static SavingGoalDto ToDto(SavingGoal goal, decimal cashAccountBalance, decimal savingCashflowAmount)
    {
        var currentAmount = Math.Max(cashAccountBalance, savingCashflowAmount);
        var remainingAmount = Math.Max(0, goal.TargetAmount - currentAmount);
        var progress = goal.TargetAmount > 0 ? Math.Min(currentAmount / goal.TargetAmount * 100, 100) : 0;
        var daysRemaining = Math.Max(0, (goal.Deadline.Date - DateTime.UtcNow.Date).Days);
        var monthsRemaining = Math.Max(1, (int)Math.Ceiling(daysRemaining / 30m));
        var monthlyRequired = goal.IsCompleted || remainingAmount == 0 ? 0 : remainingAmount / monthsRemaining;

        return new SavingGoalDto(
            goal.Id,
            goal.PortfolioId,
            goal.Portfolio.Name,
            goal.CashAccountId,
            goal.CashflowCategoryId,
            goal.CashflowCategory.Name,
            goal.Name,
            goal.Description,
            goal.TargetAmount,
            goal.Currency,
            goal.Deadline,
            goal.CreatedAt,
            goal.IsCompleted,
            cashAccountBalance,
            savingCashflowAmount,
            currentAmount,
            remainingAmount,
            progress,
            monthlyRequired,
            daysRemaining);
    }
}

public sealed record SaveSavingGoalCommand(Guid? Id, SaveSavingGoalRequest Request) : IRequest<Guid>;

public sealed class SaveSavingGoalHandler : IRequestHandler<SaveSavingGoalCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public SaveSavingGoalHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(SaveSavingGoalCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var request = command.Request;
        var currency = NormalizeCurrency(request.Currency);
        if (request.TargetAmount <= 0) throw new ResourceConflictException("Target amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ResourceConflictException("Goal name is required.");

        var ownsPortfolio = await _dbContext.Portfolios
            .AnyAsync(p => p.Id == request.PortfolioId && p.UserId == userId, cancellationToken);
        if (!ownsPortfolio) throw new ResourceNotFoundException("Portfolio not found.");

        var category = await _dbContext.CashflowCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CashflowCategoryId &&
                c.Type == CashflowType.Saving &&
                (c.IsGlobal || c.UserId == userId), cancellationToken);
        if (category == null) throw new ResourceNotFoundException("Saving cashflow category not found.");

        if (request.CashAccountId.HasValue)
        {
            var ownsCashAccount = await _dbContext.CashAccounts
                .AnyAsync(a => a.Id == request.CashAccountId.Value &&
                    a.PortfolioId == request.PortfolioId &&
                    a.Currency == currency &&
                    a.Portfolio.UserId == userId, cancellationToken);
            if (!ownsCashAccount) throw new ResourceNotFoundException("Cash account not found.");
        }

        SavingGoal goal;
        if (command.Id.HasValue)
        {
            goal = await _dbContext.SavingGoals
                .FirstOrDefaultAsync(g => g.Id == command.Id.Value && g.UserId == userId, cancellationToken)
                ?? throw new ResourceNotFoundException("Saving goal not found.");
        }
        else
        {
            goal = new SavingGoal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.SavingGoals.Add(goal);
        }

        goal.PortfolioId = request.PortfolioId;
        goal.CashAccountId = request.CashAccountId;
        goal.CashflowCategoryId = request.CashflowCategoryId;
        goal.Name = request.Name.Trim();
        goal.Description = request.Description.Trim();
        goal.TargetAmount = request.TargetAmount;
        goal.Currency = currency;
        goal.Deadline = DateTime.SpecifyKind(request.Deadline.Date, DateTimeKind.Utc);
        goal.IsCompleted = request.IsCompleted;
        goal.CompletedAt = request.IsCompleted ? goal.CompletedAt ?? DateTime.UtcNow : null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return goal.Id;
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant();
        return normalized is "VND" or "USD" ? normalized : throw new ResourceConflictException("Currency must be VND or USD.");
    }
}

public sealed record DeleteSavingGoalCommand(Guid Id) : IRequest;

public sealed class DeleteSavingGoalHandler : IRequestHandler<DeleteSavingGoalCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DeleteSavingGoalHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteSavingGoalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var goal = await _dbContext.SavingGoals
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.UserId == userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Saving goal not found.");

        _dbContext.SavingGoals.Remove(goal);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
