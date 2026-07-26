using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;
using CorePortfolio.API.Features.Admin.ControlPlane;
using CorePortfolio.API.Common;

namespace CorePortfolio.API.Features.Cashflows.CreateCashflowCategory;

public record CreateCashflowCategoryCommand(string Name, int Type, string Icon, string Color, bool IsGlobal = false, int SortOrder = 0, Guid? ParentCategoryId = null) : IRequest<Guid>;

public class CreateCashflowCategoryHandler : IRequestHandler<CreateCashflowCategoryCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateCashflowCategoryHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateCashflowCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.IsGlobal && !AdminPermissionCatalog.Has(
                _currentUserService.Role,
                AdminPermissionCatalog.SettingsManage))
        {
            throw new ForbiddenAccessException("Settings.Manage permission is required to create global categories.");
        }

        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _dbContext.CashflowCategories.FirstOrDefaultAsync(c => c.Id == request.ParentCategoryId.Value, cancellationToken);
            if (parent == null)
                throw new ArgumentException("Parent category not found.");

            if (parent.Type != (CashflowType)request.Type)
                throw new ArgumentException("Sub-category type must match parent category type.");

            if (parent.ParentCategoryId.HasValue)
                throw new ArgumentException("Maximum nesting depth is 1 (cannot create sub-category for a sub-category).");
        }

        var category = new CashflowCategory
        {
            Name = request.Name,
            Type = (CashflowType)request.Type,
            Icon = request.Icon,
            Color = request.Color,
            IsGlobal = request.IsGlobal,
            SortOrder = request.SortOrder,
            ParentCategoryId = request.ParentCategoryId,
            UserId = request.IsGlobal ? null : userId
        };

        _dbContext.CashflowCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
