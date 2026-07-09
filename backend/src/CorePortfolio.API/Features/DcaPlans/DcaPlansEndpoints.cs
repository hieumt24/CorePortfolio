using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.DcaPlans;

public static class DcaPlansEndpoints
{
    public static void MapDcaPlansEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dca-plans")
            .RequireAuthorization()
            .WithTags("DCA Plans");

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetDcaPlansQuery())));

        group.MapGet("/market-assets", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetDcaMarketAssetsQuery())));

        group.MapPost("/", async ([FromBody] SaveDcaPlanRequest request, IMediator mediator) =>
        {
            var id = await mediator.Send(new SaveDcaPlanCommand(null, request));
            return Results.Ok(new { Id = id });
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] SaveDcaPlanRequest request, IMediator mediator) =>
        {
            await mediator.Send(new SaveDcaPlanCommand(id, request));
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteDcaPlanCommand(id));
            return Results.NoContent();
        });
    }
}

public sealed record SaveDcaPlanRequest(
    Guid PortfolioId,
    Guid MarketAssetId,
    decimal Amount,
    string Currency,
    DcaFrequency Frequency,
    DateTime StartDate,
    DateTime NextExecutionDate,
    DateTime? EndDate,
    bool IsActive,
    string Notes);

public sealed record DcaPlanDto(
    Guid Id,
    Guid PortfolioId,
    string PortfolioName,
    Guid MarketAssetId,
    string Symbol,
    string AssetName,
    string CategoryName,
    decimal CurrentPrice,
    decimal Amount,
    string Currency,
    DcaFrequency Frequency,
    DateTime StartDate,
    DateTime NextExecutionDate,
    DateTime? EndDate,
    bool IsActive,
    string Notes,
    decimal EstimatedQuantity,
    decimal CashBalance,
    bool HasEnoughCash,
    List<DateTime> UpcomingExecutions);

public sealed record DcaMarketAssetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Symbol,
    string Name,
    decimal CurrentPrice,
    string Currency);

public sealed record GetDcaMarketAssetsQuery : IRequest<List<DcaMarketAssetDto>>;

public sealed class GetDcaMarketAssetsHandler : IRequestHandler<GetDcaMarketAssetsQuery, List<DcaMarketAssetDto>>
{
    private readonly AppDbContext _dbContext;

    public GetDcaMarketAssetsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DcaMarketAssetDto>> Handle(GetDcaMarketAssetsQuery request, CancellationToken cancellationToken) =>
        await _dbContext.MarketAssets
            .AsNoTracking()
            .Include(a => a.Category)
            .OrderBy(a => a.Symbol)
            .Select(a => new DcaMarketAssetDto(
                a.Id,
                a.CategoryId,
                a.Category!.Name,
                a.Symbol,
                a.Name,
                a.CurrentPrice,
                a.Category.DefaultCurrency))
            .ToListAsync(cancellationToken);
}

public sealed record GetDcaPlansQuery : IRequest<List<DcaPlanDto>>;

public sealed class GetDcaPlansHandler : IRequestHandler<GetDcaPlansQuery, List<DcaPlanDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetDcaPlansHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<DcaPlanDto>> Handle(GetDcaPlansQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var plans = await _dbContext.DcaPlans
            .AsNoTracking()
            .Include(p => p.Portfolio)
            .Include(p => p.MarketAsset)
            .ThenInclude(a => a.Category)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.NextExecutionDate)
            .ToListAsync(cancellationToken);

        var result = new List<DcaPlanDto>();
        foreach (var plan in plans)
        {
            var cashBalance = await _dbContext.CashLedgerEntries
                .AsNoTracking()
                .Where(e => e.CashAccount.PortfolioId == plan.PortfolioId &&
                    e.CashAccount.Portfolio.UserId == userId &&
                    e.CashAccount.Currency == plan.Currency)
                .SumAsync(e => e.Amount, cancellationToken);

            result.Add(new DcaPlanDto(
                plan.Id,
                plan.PortfolioId,
                plan.Portfolio.Name,
                plan.MarketAssetId,
                plan.MarketAsset.Symbol,
                plan.MarketAsset.Name,
                plan.MarketAsset.Category?.Name ?? string.Empty,
                plan.MarketAsset.CurrentPrice,
                plan.Amount,
                plan.Currency,
                plan.Frequency,
                plan.StartDate,
                plan.NextExecutionDate,
                plan.EndDate,
                plan.IsActive,
                plan.Notes,
                plan.MarketAsset.CurrentPrice > 0 ? plan.Amount / plan.MarketAsset.CurrentPrice : 0,
                cashBalance,
                cashBalance >= plan.Amount,
                GetUpcomingExecutions(plan.NextExecutionDate, plan.EndDate, plan.Frequency, 6)));
        }

        return result;
    }

    private static List<DateTime> GetUpcomingExecutions(DateTime nextExecutionDate, DateTime? endDate, DcaFrequency frequency, int count)
    {
        var dates = new List<DateTime>();
        var cursor = nextExecutionDate.Date;
        while (dates.Count < count)
        {
            if (endDate.HasValue && cursor > endDate.Value.Date) break;
            dates.Add(DateTime.SpecifyKind(cursor, DateTimeKind.Utc));
            cursor = frequency switch
            {
                DcaFrequency.Weekly => cursor.AddDays(7),
                DcaFrequency.Quarterly => cursor.AddMonths(3),
                _ => cursor.AddMonths(1)
            };
        }

        return dates;
    }
}

public sealed record SaveDcaPlanCommand(Guid? Id, SaveDcaPlanRequest Request) : IRequest<Guid>;

public sealed class SaveDcaPlanHandler : IRequestHandler<SaveDcaPlanCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public SaveDcaPlanHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(SaveDcaPlanCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var request = command.Request;
        var currency = NormalizeCurrency(request.Currency);
        if (request.Amount <= 0) throw new ResourceConflictException("DCA amount must be greater than zero.");

        var ownsPortfolio = await _dbContext.Portfolios
            .AnyAsync(p => p.Id == request.PortfolioId && p.UserId == userId, cancellationToken);
        if (!ownsPortfolio) throw new ResourceNotFoundException("Portfolio not found.");

        var marketAssetExists = await _dbContext.MarketAssets
            .AnyAsync(a => a.Id == request.MarketAssetId, cancellationToken);
        if (!marketAssetExists) throw new ResourceNotFoundException("Market asset not found.");

        DcaPlan plan;
        if (command.Id.HasValue)
        {
            plan = await _dbContext.DcaPlans
                .FirstOrDefaultAsync(p => p.Id == command.Id.Value && p.UserId == userId, cancellationToken)
                ?? throw new ResourceNotFoundException("DCA plan not found.");
        }
        else
        {
            plan = new DcaPlan
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.DcaPlans.Add(plan);
        }

        plan.PortfolioId = request.PortfolioId;
        plan.MarketAssetId = request.MarketAssetId;
        plan.Amount = request.Amount;
        plan.Currency = currency;
        plan.Frequency = request.Frequency;
        plan.StartDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        plan.NextExecutionDate = DateTime.SpecifyKind(request.NextExecutionDate.Date, DateTimeKind.Utc);
        plan.EndDate = request.EndDate.HasValue ? DateTime.SpecifyKind(request.EndDate.Value.Date, DateTimeKind.Utc) : null;
        plan.IsActive = request.IsActive;
        plan.Notes = request.Notes.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return plan.Id;
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant();
        return normalized is "VND" or "USD" ? normalized : throw new ResourceConflictException("Currency must be VND or USD.");
    }
}

public sealed record DeleteDcaPlanCommand(Guid Id) : IRequest;

public sealed class DeleteDcaPlanHandler : IRequestHandler<DeleteDcaPlanCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DeleteDcaPlanHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteDcaPlanCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var plan = await _dbContext.DcaPlans
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.UserId == userId, cancellationToken)
            ?? throw new ResourceNotFoundException("DCA plan not found.");

        _dbContext.DcaPlans.Remove(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
