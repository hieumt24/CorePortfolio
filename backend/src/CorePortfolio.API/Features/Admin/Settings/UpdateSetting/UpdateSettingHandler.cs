using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.Admin.Settings.UpdateSetting;

public class UpdateSettingHandler : IRequestHandler<UpdateSettingCommand, bool>
{
    private readonly AppDbContext _dbContext;

    public UpdateSettingHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == request.Key, cancellationToken);
            
        if (setting == null)
        {
            // Or we could create it if it doesn't exist
            setting = new Domain.Entities.SystemSetting
            {
                Key = request.Key,
                Value = request.Value,
                LastUpdated = DateTime.UtcNow
            };
            _dbContext.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = request.Value;
            setting.LastUpdated = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
