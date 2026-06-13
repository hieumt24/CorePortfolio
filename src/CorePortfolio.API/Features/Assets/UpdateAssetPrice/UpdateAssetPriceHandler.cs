using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Assets.UpdateAssetPrice;

public class UpdateAssetPriceHandler : IRequestHandler<UpdateAssetPriceCommand>
{
    private readonly AppDbContext _dbContext;

    public UpdateAssetPriceHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateAssetPriceCommand request, CancellationToken cancellationToken)
    {
        var asset = await _dbContext.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken);
        
        if (asset == null)
            throw new Exception("Asset not found"); // In a real app, use proper exception or Result pattern

        asset.CurrentPrice = request.NewPrice;
        
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
