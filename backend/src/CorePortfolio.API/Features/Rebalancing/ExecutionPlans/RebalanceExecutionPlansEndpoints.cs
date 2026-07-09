using CorePortfolio.API.Common;
using CorePortfolio.API.Features.Rebalancing.GetRebalanceSuggestions;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Rebalancing.ExecutionPlans;

public static class RebalanceExecutionPlansEndpoints
{
    public static void MapRebalanceExecutionPlansEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rebalancing/plans")
            .RequireAuthorization()
            .WithTags("Rebalancing");

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetRebalanceExecutionPlansQuery())));

        group.MapPost("/simulate", async ([FromBody] SimulateRebalanceExecutionPlanRequest request, IMediator mediator) =>
            Results.Ok(await mediator.Send(new SimulateRebalanceExecutionPlanCommand(request.Currency))));

        group.MapPost("/{id:guid}/apply", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new ApplyRebalanceExecutionPlanCommand(id));
            return Results.NoContent();
        });
    }
}

public sealed record SimulateRebalanceExecutionPlanRequest(string Currency);

public sealed record RebalanceExecutionPlanDto(
    Guid Id,
    string Currency,
    RebalanceExecutionPlanStatus Status,
    decimal AvailableCash,
    DateTime CreatedAt,
    DateTime? AppliedAt,
    string Notes,
    List<RebalanceExecutionPlanItemDto> Items);

public sealed record RebalanceExecutionPlanItemDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    RebalanceExecutionAction Action,
    decimal CurrentValue,
    decimal TargetValue,
    decimal SuggestedAmount,
    decimal ExecutableAmount,
    bool IsCashLimited,
    int Priority);

public sealed record GetRebalanceExecutionPlansQuery : IRequest<List<RebalanceExecutionPlanDto>>;

public sealed class GetRebalanceExecutionPlansHandler : IRequestHandler<GetRebalanceExecutionPlansQuery, List<RebalanceExecutionPlanDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetRebalanceExecutionPlansHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<RebalanceExecutionPlanDto>> Handle(GetRebalanceExecutionPlansQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var plans = await _dbContext.RebalanceExecutionPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .ThenInclude(i => i.Category)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        return plans.Select(ToDto).ToList();
    }

    private static RebalanceExecutionPlanDto ToDto(RebalanceExecutionPlan plan) =>
        new(
            plan.Id,
            plan.Currency,
            plan.Status,
            plan.AvailableCash,
            plan.CreatedAt,
            plan.AppliedAt,
            plan.Notes,
            plan.Items
                .OrderBy(i => i.Priority)
                .Select(i => new RebalanceExecutionPlanItemDto(
                    i.Id,
                    i.CategoryId,
                    i.Category.Name,
                    i.Action,
                    i.CurrentValue,
                    i.TargetValue,
                    i.SuggestedAmount,
                    i.ExecutableAmount,
                    i.IsCashLimited,
                    i.Priority))
                .ToList());
}

public sealed record SimulateRebalanceExecutionPlanCommand(string Currency) : IRequest<RebalanceExecutionPlanDto>;

public sealed class SimulateRebalanceExecutionPlanHandler : IRequestHandler<SimulateRebalanceExecutionPlanCommand, RebalanceExecutionPlanDto>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public SimulateRebalanceExecutionPlanHandler(AppDbContext dbContext, ICurrentUserService currentUserService, IMediator mediator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<RebalanceExecutionPlanDto> Handle(SimulateRebalanceExecutionPlanCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var currency = NormalizeCurrency(request.Currency);
        var suggestions = await _mediator.Send(new GetRebalanceSuggestionsQuery(userId, currency), cancellationToken);
        var availableCash = await _dbContext.CashLedgerEntries
            .AsNoTracking()
            .Where(e => e.CashAccount.Portfolio.UserId == userId && e.CashAccount.Currency == currency)
            .SumAsync(e => e.Amount, cancellationToken);

        var plan = new RebalanceExecutionPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Currency = currency,
            AvailableCash = availableCash,
            CreatedAt = DateTime.UtcNow,
            Status = RebalanceExecutionPlanStatus.Simulated
        };

        var sellSuggestions = suggestions
            .Where(s => s.Action == "Sell")
            .OrderByDescending(s => s.DifferenceValue)
            .ToList();
        var buySuggestions = suggestions
            .Where(s => s.Action == "Buy")
            .OrderByDescending(s => s.DifferenceValue)
            .ToList();

        var availableToBuy = availableCash + sellSuggestions.Sum(s => s.DifferenceValue);
        var priority = 1;
        foreach (var suggestion in sellSuggestions)
        {
            plan.Items.Add(CreateItem(plan.Id, suggestion, RebalanceExecutionAction.Sell, suggestion.DifferenceValue, false, priority++));
        }

        foreach (var suggestion in buySuggestions)
        {
            var executableAmount = Math.Min(suggestion.DifferenceValue, Math.Max(0, availableToBuy));
            plan.Items.Add(CreateItem(plan.Id, suggestion, RebalanceExecutionAction.Buy, executableAmount, executableAmount < suggestion.DifferenceValue, priority++));
            availableToBuy -= executableAmount;
        }

        if (plan.Items.Count == 0)
        {
            plan.Notes = "Portfolio is already within the target allocation tolerance.";
        }
        else if (plan.Items.Any(i => i.IsCashLimited))
        {
            plan.Notes = "Some buy legs are limited by available cash and planned sells.";
        }

        _dbContext.RebalanceExecutionPlans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedPlan = await _dbContext.RebalanceExecutionPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .ThenInclude(i => i.Category)
            .SingleAsync(p => p.Id == plan.Id, cancellationToken);

        return ToDto(savedPlan);
    }

    private static RebalanceExecutionPlanItem CreateItem(
        Guid planId,
        RebalanceSuggestionDto suggestion,
        RebalanceExecutionAction action,
        decimal executableAmount,
        bool isCashLimited,
        int priority) =>
        new()
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            CategoryId = suggestion.CategoryId,
            Action = action,
            CurrentValue = suggestion.CurrentValue,
            TargetValue = suggestion.TargetValue,
            SuggestedAmount = suggestion.DifferenceValue,
            ExecutableAmount = executableAmount,
            IsCashLimited = isCashLimited,
            Priority = priority
        };

    private static RebalanceExecutionPlanDto ToDto(RebalanceExecutionPlan plan) =>
        new(
            plan.Id,
            plan.Currency,
            plan.Status,
            plan.AvailableCash,
            plan.CreatedAt,
            plan.AppliedAt,
            plan.Notes,
            plan.Items
                .OrderBy(i => i.Priority)
                .Select(i => new RebalanceExecutionPlanItemDto(
                    i.Id,
                    i.CategoryId,
                    i.Category.Name,
                    i.Action,
                    i.CurrentValue,
                    i.TargetValue,
                    i.SuggestedAmount,
                    i.ExecutableAmount,
                    i.IsCashLimited,
                    i.Priority))
                .ToList());

    private static string NormalizeCurrency(string currency)
    {
        var normalized = string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant();
        return normalized is "VND" or "USD" ? normalized : throw new ResourceConflictException("Currency must be VND or USD.");
    }
}

public sealed record ApplyRebalanceExecutionPlanCommand(Guid Id) : IRequest;

public sealed class ApplyRebalanceExecutionPlanHandler : IRequestHandler<ApplyRebalanceExecutionPlanCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public ApplyRebalanceExecutionPlanHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(ApplyRebalanceExecutionPlanCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var plan = await _dbContext.RebalanceExecutionPlans
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.UserId == userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Rebalance plan not found.");

        plan.Status = RebalanceExecutionPlanStatus.Applied;
        plan.AppliedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
