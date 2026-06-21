using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.GetCashflowCategories;

public record CashflowCategoryDto(Guid Id, string Name, int Type, string Icon, string Color, bool IsGlobal);

public record GetCashflowCategoriesQuery : IRequest<List<CashflowCategoryDto>>;

public class GetCashflowCategoriesHandler : IRequestHandler<GetCashflowCategoriesQuery, List<CashflowCategoryDto>>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCashflowCategoriesHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<CashflowCategoryDto>> Handle(GetCashflowCategoriesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId.Value;

        var categories = await _dbContext.CashflowCategories
            .Where(c => c.IsGlobal || c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .Select(c => new CashflowCategoryDto(c.Id, c.Name, (int)c.Type, c.Icon, c.Color, c.IsGlobal))
            .ToListAsync(cancellationToken);

        return categories;
    }
}
