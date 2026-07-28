using CorePortfolio.API.Features.RecurringCashflows.CreateRecurringCashflowRule;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
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
        group.MapPost("", async (
            [FromBody] RecurringCashflowRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new CreateRecurringCashflowRuleCommand(request),
                cancellationToken)));
        group.MapPatch("/{id:guid}/toggle", async (Guid id, AppDbContext db, ICurrentUserService current) =>
        {
            var rule = await db.RecurringCashflowRules.FirstOrDefaultAsync(r => r.Id == id && r.UserId == current.UserId);
            if (rule is null) return Results.NotFound(); rule.IsActive = !rule.IsActive; await db.SaveChangesAsync(); return Results.Ok(ToDto(rule));
        });
    }

    private static RecurringCashflowDto ToDto(RecurringCashflowRule r) =>
        RecurringCashflowMappings.ToDto(r);
}

public record RecurringCashflowRequest(Guid PortfolioId, Guid CategoryId, decimal Amount, string Currency, string Frequency, DateTime NextOccurrence, DateTime? EndDate, string Description);
public record RecurringCashflowDto(Guid Id, Guid PortfolioId, Guid CategoryId, decimal Amount, string Currency, string Frequency, DateTime NextOccurrence, DateTime? EndDate, string Description, bool IsActive, DateTime? LastGeneratedAt);

internal static class RecurringCashflowMappings
{
    public static RecurringCashflowDto ToDto(RecurringCashflowRule rule) =>
        new(
            rule.Id,
            rule.PortfolioId,
            rule.CategoryId,
            rule.Amount,
            rule.Currency,
            rule.Frequency,
            rule.NextOccurrence,
            rule.EndDate,
            rule.Description,
            rule.IsActive,
            rule.LastGeneratedAt);
}
