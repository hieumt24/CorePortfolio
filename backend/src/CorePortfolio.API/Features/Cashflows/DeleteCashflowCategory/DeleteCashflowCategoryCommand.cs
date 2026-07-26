using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using CorePortfolio.API.Services;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Features.Admin.ControlPlane;
using CorePortfolio.API.Common;

namespace CorePortfolio.API.Features.Cashflows.DeleteCashflowCategory;

public record DeleteCashflowCategoryCommand(Guid Id) : IRequest;

public class DeleteCashflowCategoryHandler : IRequestHandler<DeleteCashflowCategoryCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCashflowCategoryHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteCashflowCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.CashflowCategories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        
        if (category == null)
            throw new Exception("Category not found");

        if (category.IsGlobal && !AdminPermissionCatalog.Has(
                _currentUserService.Role,
                AdminPermissionCatalog.SettingsManage))
        {
            throw new ForbiddenAccessException("Settings.Manage permission is required to delete global categories.");
        }

        if (!category.IsGlobal && category.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You can only delete your own categories.");
        }

        var hasRecords = await _dbContext.CashflowRecords.AnyAsync(r => r.CategoryId == request.Id, cancellationToken);
        if (hasRecords)
        {
            throw new Exception("Cannot delete category because it has associated cashflow records.");
        }

        _dbContext.CashflowCategories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
