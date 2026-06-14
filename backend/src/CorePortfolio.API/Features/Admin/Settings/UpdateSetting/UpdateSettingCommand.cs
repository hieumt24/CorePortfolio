using MediatR;

namespace CorePortfolio.API.Features.Admin.Settings.UpdateSetting;

public record UpdateSettingCommand(string Key, string Value) : IRequest<bool>;
