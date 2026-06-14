using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.Categories;

public record UpdateCategoryCommand(Guid Id, string Name, string DefaultCurrency) : IRequest<bool>;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, bool>
{
    private readonly AppDbContext _dbContext;

    public UpdateCategoryHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.AssetCategories.FindAsync(new object[] { request.Id }, cancellationToken);
        if (category == null)
            return false;

        category.Name = request.Name;
        category.DefaultCurrency = request.DefaultCurrency;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
