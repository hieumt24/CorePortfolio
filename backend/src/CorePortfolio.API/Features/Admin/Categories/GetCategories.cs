using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.Categories;

public record CategoryDto(Guid Id, string Name, string DefaultCurrency);

public record GetCategoriesQuery() : IRequest<List<CategoryDto>>;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly AppDbContext _dbContext;
    public GetCategoriesHandler(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.AssetCategories
            .AsNoTracking()
            .Select(c => new CategoryDto(c.Id, c.Name, c.DefaultCurrency))
            .ToListAsync(cancellationToken);
    }
}
