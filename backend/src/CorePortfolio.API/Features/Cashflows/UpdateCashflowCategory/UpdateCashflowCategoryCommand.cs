using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using CorePortfolio.API.Services;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Cashflows.UpdateCashflowCategory;

public record UpdateCashflowCategoryCommand(Guid Id, string Name, int Type, string Icon, string Color, int SortOrder = 0, Guid? ParentCategoryId = null) : IRequest;

public class UpdateCashflowCategoryHandler : IRequestHandler<UpdateCashflowCategoryCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCashflowCategoryHandler(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateCashflowCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.CashflowCategories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        
        if (category == null)
            throw new Exception("Category not found");

        if (category.IsGlobal && !_currentUserService.IsAdmin)
        {
            throw new UnauthorizedAccessException("Only admins can update global categories.");
        }

        if (!category.IsGlobal && category.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You can only update your own categories.");
        }

        if (request.ParentCategoryId == request.Id)
        {
            throw new ArgumentException("A category cannot be its own parent.");
        }

        var hasChildren = await _dbContext.CashflowCategories
            .AnyAsync(c => c.ParentCategoryId == request.Id, cancellationToken);
        if (hasChildren && request.ParentCategoryId.HasValue)
        {
            throw new ArgumentException("A parent category with children cannot become a sub-category.");
        }

        if (hasChildren && category.Type != (CashflowType)request.Type)
        {
            throw new ArgumentException("A parent category with children cannot change type.");
        }

        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _dbContext.CashflowCategories
                .FirstOrDefaultAsync(c => c.Id == request.ParentCategoryId.Value, cancellationToken);
            if (parent == null)
                throw new ArgumentException("Parent category not found.");

            if (parent.Type != (CashflowType)request.Type)
                throw new ArgumentException("Sub-category type must match parent category type.");

            if (parent.ParentCategoryId.HasValue)
                throw new ArgumentException("Maximum nesting depth is 1 (cannot assign a sub-category as parent).");
        }

        category.Name = request.Name;
        category.Type = (CashflowType)request.Type;
        category.Icon = request.Icon;
        category.Color = request.Color;
        category.SortOrder = request.SortOrder;
        category.ParentCategoryId = request.ParentCategoryId;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
