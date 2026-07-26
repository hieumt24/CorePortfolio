using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record CreateMarketAssetCommand(Guid CategoryId, string Symbol, string Name, decimal CurrentPrice,
    string PriceSource = "Manual", string? ExternalId = null) : IRequest<Guid>, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.MarketDataManage;
}

public class CreateMarketAssetHandler : IRequestHandler<CreateMarketAssetCommand, Guid>
{
    private readonly AppDbContext _dbContext;
    private readonly AuditWriter _auditWriter;
    public CreateMarketAssetHandler(AppDbContext dbContext, AuditWriter auditWriter)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
    }

    public async Task<Guid> Handle(CreateMarketAssetCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await _dbContext.AssetCategories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists) throw new Exception("Category not found");

        var marketAsset = new MarketAsset
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Symbol = request.Symbol,
            Name = request.Name,
            CurrentPrice = request.CurrentPrice,
            LastUpdated = DateTime.UtcNow,
            PriceSource = request.PriceSource,
            ExternalId = request.ExternalId,
            PriceStatus = "Manual"
        };
        _dbContext.MarketAssets.Add(marketAsset);
        _auditWriter.Add(
            "MarketAssetCreated",
            "MarketAsset",
            marketAsset.Id.ToString(),
            new { marketAsset.Symbol, marketAsset.Name, marketAsset.PriceSource });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return marketAsset.Id;
    }
}
