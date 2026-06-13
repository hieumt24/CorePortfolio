using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.Settings.GetSetting;

public class GetSettingHandler : IRequestHandler<GetSettingQuery, string?>
{
    private readonly AppDbContext _dbContext;

    public GetSettingHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> Handle(GetSettingQuery request, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == request.Key, cancellationToken);
            
        return setting?.Value;
    }
}
