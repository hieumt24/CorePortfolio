using MediatR;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.Admin.Settings.UpdateSetting;

public record UpdateSettingCommand(string Key, string Value) : IRequest<bool>, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.SettingsManage;
}
