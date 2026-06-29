using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.GetCashflowCategories;

public record CashflowCategoryDto(Guid Id, string Name, int Type, string Icon, string Color, bool IsGlobal, int SortOrder, Guid? ParentCategoryId, List<CashflowCategoryDto> SubCategories);

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

        var allCategories = await _dbContext.CashflowCategories
            .Where(c => c.IsGlobal || c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var topLevelCategories = allCategories.Where(c => c.ParentCategoryId == null).ToList();

        return topLevelCategories.Select(c => MapToDto(c, allCategories)).ToList();
    }

    private CashflowCategoryDto MapToDto(CashflowCategory category, List<CashflowCategory> allCategories)
    {
        var subCategories = allCategories
            .Where(c => c.ParentCategoryId == category.Id)
            .Select(c => MapToDto(c, allCategories))
            .ToList();

        return new CashflowCategoryDto(
            category.Id, 
            category.Name, 
            (int)category.Type, 
            category.Icon, 
            category.Color, 
            category.IsGlobal, 
            category.SortOrder, 
            category.ParentCategoryId, 
            subCategories);
    }
}
