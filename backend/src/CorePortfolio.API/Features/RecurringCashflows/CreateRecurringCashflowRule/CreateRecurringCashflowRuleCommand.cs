using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.RecurringCashflows.CreateRecurringCashflowRule;

public sealed record CreateRecurringCashflowRuleCommand(
    RecurringCashflowRequest Request) : IRequest<RecurringCashflowDto>;

public sealed class CreateRecurringCashflowRuleHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateRecurringCashflowRuleCommand, RecurringCashflowDto>
{
    private static readonly IReadOnlyDictionary<string, string> SupportedFrequencies =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Daily"] = "Daily",
            ["Weekly"] = "Weekly",
            ["Monthly"] = "Monthly",
            ["Quarterly"] = "Quarterly",
            ["Yearly"] = "Yearly"
        };

    public async Task<RecurringCashflowDto> Handle(
        CreateRecurringCashflowRuleCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var request = command.Request;

        if (request.Amount <= 0)
            throw new RequestValidationException("Recurring cashflow amount must be greater than zero.");

        var currency = NormalizeCurrency(request.Currency);
        var frequency = NormalizeFrequency(request.Frequency);
        var nextOccurrence = NormalizeDate(request.NextOccurrence, "Next occurrence");
        DateTime? endDate = request.EndDate.HasValue
            ? NormalizeDate(request.EndDate.Value, "End date")
            : null;

        if (endDate.HasValue && endDate.Value < nextOccurrence)
            throw new RequestValidationException("End date cannot be earlier than the next occurrence.");

        var ownsPortfolio = await dbContext.Portfolios
            .AnyAsync(
                portfolio => portfolio.Id == request.PortfolioId && portfolio.UserId == userId,
                cancellationToken);
        if (!ownsPortfolio)
            throw new ResourceNotFoundException("Portfolio not found.");

        var canUseCategory = await dbContext.CashflowCategories
            .AsNoTracking()
            .AnyAsync(
                category => category.Id == request.CategoryId &&
                    (category.IsGlobal || category.UserId == userId),
                cancellationToken);
        if (!canUseCategory)
            throw new ResourceNotFoundException("Cashflow category not found.");

        var rule = new RecurringCashflowRule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PortfolioId = request.PortfolioId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Currency = currency,
            Frequency = frequency,
            NextOccurrence = nextOccurrence,
            EndDate = endDate,
            Description = request.Description?.Trim() ?? string.Empty,
            IsActive = true
        };

        dbContext.RecurringCashflowRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RecurringCashflowMappings.ToDto(rule);
    }

    private static string NormalizeCurrency(string? currency)
    {
        var normalized = currency?.Trim().ToUpperInvariant();
        return normalized is "VND" or "USD"
            ? normalized
            : throw new RequestValidationException("Currency must be VND or USD.");
    }

    private static string NormalizeFrequency(string? frequency)
    {
        var normalized = frequency?.Trim();
        if (normalized is not null && SupportedFrequencies.TryGetValue(normalized, out var canonical))
            return canonical;

        throw new RequestValidationException(
            "Frequency must be Daily, Weekly, Monthly, Quarterly, or Yearly.");
    }

    private static DateTime NormalizeDate(DateTime value, string fieldName)
    {
        if (value == default)
            throw new RequestValidationException($"{fieldName} is required.");

        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }
}
