using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record DeleteMarketAssetCommand(Guid Id) : IRequest<bool>, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.MarketDataManage;
}

public class DeleteMarketAssetHandler : IRequestHandler<DeleteMarketAssetCommand, bool>
{
    private readonly AppDbContext _dbContext;
    private readonly AuditWriter _auditWriter;

    public DeleteMarketAssetHandler(AppDbContext dbContext, AuditWriter auditWriter)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
    }

    public async Task<bool> Handle(DeleteMarketAssetCommand request, CancellationToken cancellationToken)
    {
        var marketAsset = await _dbContext.MarketAssets.FindAsync(new object[] { request.Id }, cancellationToken);
        if (marketAsset == null)
            return false;

        try
        {
            _dbContext.MarketAssets.Remove(marketAsset);
            _auditWriter.Add(
                "MarketAssetDeleted",
                "MarketAsset",
                marketAsset.Id.ToString(),
                new { marketAsset.Symbol, marketAsset.Name });
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Cannot delete this market asset because it is currently included in one or more user portfolios.");
        }
    }
}
