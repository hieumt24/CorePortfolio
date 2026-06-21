using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using CorePortfolio.API.Services;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Cashflows.UpdateCashflowCategory;

public record UpdateCashflowCategoryCommand(Guid Id, string Name, int Type, string Icon, string Color) : IRequest;

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

        category.Name = request.Name;
        category.Type = (CashflowType)request.Type;
        category.Icon = request.Icon;
        category.Color = request.Color;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
