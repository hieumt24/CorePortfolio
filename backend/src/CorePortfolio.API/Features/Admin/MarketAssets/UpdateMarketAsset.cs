using CorePortfolio.Infrastructure.Data;
using MediatR;
using CorePortfolio.API.Services;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record UpdateMarketAssetCommand(Guid Id, Guid CategoryId, string Symbol, string Name, decimal CurrentPrice,
    string PriceSource = "Manual", string? ExternalId = null) : IRequest<bool>, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.MarketDataManage;
}

public class UpdateMarketAssetHandler : IRequestHandler<UpdateMarketAssetCommand, bool>
{
    private readonly AppDbContext _dbContext;
    private readonly AuditWriter _auditWriter;

    public UpdateMarketAssetHandler(AppDbContext dbContext, AuditWriter auditWriter)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
    }

    public async Task<bool> Handle(UpdateMarketAssetCommand request, CancellationToken cancellationToken)
    {
        var marketAsset = await _dbContext.MarketAssets.FindAsync(new object[] { request.Id }, cancellationToken);
        if (marketAsset == null)
            return false;

        var previousPrice = marketAsset.CurrentPrice;
        var previousSource = marketAsset.PriceSource;
        marketAsset.CategoryId = request.CategoryId;
        marketAsset.Symbol = request.Symbol;
        marketAsset.Name = request.Name;
        marketAsset.CurrentPrice = request.CurrentPrice;
        marketAsset.PriceSource = request.PriceSource;
        marketAsset.ExternalId = request.ExternalId;
        if (request.PriceSource.Equals("Manual", StringComparison.OrdinalIgnoreCase))
        {
            marketAsset.PriceStatus = "Manual";
            marketAsset.LastPriceError = null;
            marketAsset.LastUpdated = DateTime.UtcNow;
        }

        _auditWriter.Add(
            "MarketAssetUpdated",
            "MarketAsset",
            marketAsset.Id.ToString(),
            new
            {
                marketAsset.Symbol,
                PreviousPrice = previousPrice,
                NewPrice = marketAsset.CurrentPrice,
                PreviousSource = previousSource,
                NewSource = marketAsset.PriceSource
            });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
