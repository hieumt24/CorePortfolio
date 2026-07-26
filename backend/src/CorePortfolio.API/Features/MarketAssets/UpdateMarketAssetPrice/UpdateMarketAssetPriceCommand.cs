using MediatR;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.MarketAssets.UpdateMarketAssetPrice;

public record UpdateMarketAssetPriceCommand(Guid MarketAssetId, decimal NewPrice) : IRequest, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.MarketDataManage;
}
