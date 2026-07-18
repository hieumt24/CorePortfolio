using CorePortfolio.API.Features.Budgets;
using CorePortfolio.API.Features.Cashflows.GetCashflowSummary;
using CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;
using CorePortfolio.API.Features.Portfolios.GetPortfolios;
using MediatR;

namespace CorePortfolio.API.Features.Dashboard.GetFinancialHealth;

public record FinancialHealthDto(
    decimal NetWorth,
    decimal InvestedValue,
    decimal CashBalance,
    decimal UnrealizedPnl,
    decimal MonthlyIncome,
    decimal MonthlyExpense,
    decimal MonthlyNetFlow,
    decimal BudgetLimit,
    decimal BudgetSpent,
    decimal BudgetProgressPercentage,
    int PortfolioCount,
    int BudgetWarningCount,
    int BudgetExceededCount,
    DateTime AsOf);

public record GetFinancialHealthQuery(string Currency = "VND") : IRequest<FinancialHealthDto>;

public sealed class GetFinancialHealthHandler : IRequestHandler<GetFinancialHealthQuery, FinancialHealthDto>
{
    private readonly IMediator _mediator;

    public GetFinancialHealthHandler(IMediator mediator) => _mediator = mediator;

    public async Task<FinancialHealthDto> Handle(GetFinancialHealthQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1);
        var end = start.AddMonths(1).AddTicks(-1);
        var portfolios = await _mediator.Send(new GetPortfoliosQuery(), cancellationToken);
        var summaries = await Task.WhenAll(portfolios.Select(p => _mediator.Send(new GetPortfolioSummaryQuery(p.Id), cancellationToken)));
        var valid = summaries.Where(s => s is not null).Cast<PortfolioSummaryDto>().ToList();
        var cashflow = await _mediator.Send(new GetCashflowSummaryQuery(request.Currency, start, end), cancellationToken);
        var budgets = await _mediator.Send(new GetBudgetsProgressQuery(now.Year, now.Month, request.Currency), cancellationToken);
        var invested = valid.Sum(s => s.TotalInvested);
        var portfolioValue = valid.Sum(s => s.CurrentTotalValue);
        var cash = valid.Sum(s => s.CashBalances.Where(c => c.Currency == request.Currency).Sum(c => c.Balance));
        var budgetLimit = budgets.Sum(b => b.MonthlyLimit);
        var budgetSpent = budgets.Sum(b => b.SpentAmount);
        return new FinancialHealthDto(
            portfolioValue + cash, invested, cash, valid.Sum(s => s.UnrealizedPnl),
            cashflow.TotalIncome, cashflow.TotalExpense, cashflow.NetFlow,
            budgetLimit, budgetSpent, budgetLimit == 0 ? 0 : budgetSpent / budgetLimit * 100,
            valid.Count, budgets.Count(b => !b.IsExceeded && b.RawProgressPercentage >= 80),
            budgets.Count(b => b.IsExceeded), now);
    }
}
