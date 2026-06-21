using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Cashflows.CreateCashflowCategory;

public record CreateCashflowCategoryCommand(string Name, int Type, string Icon, string Color, bool IsGlobal = false) : IRequest<Guid>;

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
        if (request.IsGlobal && !_currentUserService.IsAdmin)
        {
            throw new UnauthorizedAccessException("Only admins can create global categories.");
        }

        var userId = _currentUserService.UserId.Value;
        var category = new CashflowCategory
        {
            Name = request.Name,
            Type = (CashflowType)request.Type,
            Icon = request.Icon,
            Color = request.Color,
            IsGlobal = request.IsGlobal,
            UserId = request.IsGlobal ? null : _currentUserService.UserId
        };

        _dbContext.CashflowCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
