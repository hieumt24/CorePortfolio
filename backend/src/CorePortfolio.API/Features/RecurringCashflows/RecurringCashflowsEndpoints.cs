using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.RecurringCashflows;

public static class RecurringCashflowsEndpoints
{
    public static void MapRecurringCashflowsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recurring-cashflows").RequireAuthorization();
        group.MapGet("", async (AppDbContext db, ICurrentUserService current) =>
            Results.Ok((await db.RecurringCashflowRules.AsNoTracking().Where(r => r.UserId == current.UserId).OrderBy(r => r.NextOccurrence).ToListAsync()).Select(ToDto).ToList()));
        group.MapPost("", async (AppDbContext db, ICurrentUserService current, [FromBody] RecurringCashflowRequest request) =>
        {
            var userId = current.UserId ?? throw new UnauthorizedAccessException();
            var rule = new RecurringCashflowRule { UserId = userId, PortfolioId = request.PortfolioId, CategoryId = request.CategoryId, Amount = request.Amount, Currency = request.Currency, Frequency = request.Frequency, NextOccurrence = request.NextOccurrence, EndDate = request.EndDate, Description = request.Description };
            db.RecurringCashflowRules.Add(rule); await db.SaveChangesAsync(); return Results.Ok(ToDto(rule));
        });
        group.MapPatch("/{id:guid}/toggle", async (Guid id, AppDbContext db, ICurrentUserService current) =>
        {
            var rule = await db.RecurringCashflowRules.FirstOrDefaultAsync(r => r.Id == id && r.UserId == current.UserId);
            if (rule is null) return Results.NotFound(); rule.IsActive = !rule.IsActive; await db.SaveChangesAsync(); return Results.Ok(ToDto(rule));
        });
    }

    private static RecurringCashflowDto ToDto(RecurringCashflowRule r) => new(r.Id, r.PortfolioId, r.CategoryId, r.Amount, r.Currency, r.Frequency, r.NextOccurrence, r.EndDate, r.Description, r.IsActive, r.LastGeneratedAt);
}

public record RecurringCashflowRequest(Guid PortfolioId, Guid CategoryId, decimal Amount, string Currency, string Frequency, DateTime NextOccurrence, DateTime? EndDate, string Description);
public record RecurringCashflowDto(Guid Id, Guid PortfolioId, Guid CategoryId, decimal Amount, string Currency, string Frequency, DateTime NextOccurrence, DateTime? EndDate, string Description, bool IsActive, DateTime? LastGeneratedAt);
