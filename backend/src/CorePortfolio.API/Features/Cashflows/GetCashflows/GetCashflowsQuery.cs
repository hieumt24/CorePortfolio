using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.GetCashflows;

public record CashflowRecordDto(Guid Id, Guid PortfolioId, string PortfolioName, Guid CategoryId, string CategoryName, string CategoryIcon, string CategoryColor, int Type, decimal Amount, string Currency, DateTime Date, string Description);

public record GetCashflowsQuery(int Page = 1, int PageSize = 50, string? Currency = null, int? Type = null) : IRequest<List<CashflowRecordDto>>;

public class GetCashflowsHandler : IRequestHandler<GetCashflowsQuery, List<CashflowRecordDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCashflowsHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<CashflowRecordDto>> Handle(GetCashflowsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId.Value;

        var query = _dbContext.CashflowRecords
            .Include(c => c.Category)
            .Include(c => c.Portfolio)
            .Where(c => c.UserId == userId);

        if (!string.IsNullOrEmpty(request.Currency))
        {
            query = query.Where(c => c.Currency == request.Currency);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(c => (int)c.Category.Type == request.Type.Value);
        }

        var cashflows = await query
            .OrderByDescending(c => c.Date)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CashflowRecordDto(
                c.Id,
                c.PortfolioId,
                c.Portfolio != null ? c.Portfolio.Name : "",
                c.CategoryId,
                c.Category != null ? c.Category.Name : "",
                c.Category != null ? c.Category.Icon : "",
                c.Category != null ? c.Category.Color : "",
                c.Category != null ? (int)c.Category.Type : 0,
                c.Amount,
                c.Currency,
                c.Date,
                c.Description
            ))
            .ToListAsync(cancellationToken);

        return cashflows;
    }
}
