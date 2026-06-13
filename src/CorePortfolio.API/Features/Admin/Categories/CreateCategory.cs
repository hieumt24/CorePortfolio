using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.Categories;

public record CreateCategoryCommand(string Name, string DefaultCurrency) : IRequest<Guid>;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    public CreateCategoryHandler(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new AssetCategory
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            DefaultCurrency = request.DefaultCurrency
        };
        _dbContext.AssetCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
