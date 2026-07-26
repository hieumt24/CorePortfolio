using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.Admin.Categories;

public record DeleteCategoryCommand(Guid Id) : IRequest<bool>, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.MarketDataManage;
}

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly AppDbContext _dbContext;

    public DeleteCategoryHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.AssetCategories.FindAsync(new object[] { request.Id }, cancellationToken);
        if (category == null)
            return false;

        try
        {
            _dbContext.AssetCategories.Remove(category);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Cannot delete this category because it is being used by existing market assets.");
        }
    }
}
