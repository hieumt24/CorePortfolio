using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;

namespace CorePortfolio.API.Features.Admin.Settings.UpdateSetting;

public class UpdateSettingHandler : IRequestHandler<UpdateSettingCommand, bool>
{
    private readonly AppDbContext _dbContext;
    private readonly AuditWriter _auditWriter;

    public UpdateSettingHandler(AppDbContext dbContext, AuditWriter auditWriter)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
    }

    public async Task<bool> Handle(UpdateSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == request.Key, cancellationToken);
            
        var previousValue = setting?.Value;
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

        _auditWriter.Add(
            "SystemSettingUpdated",
            "SystemSetting",
            setting.Key,
            new { Created = previousValue is null });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
