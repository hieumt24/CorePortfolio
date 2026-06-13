using MediatR;

namespace CorePortfolio.API.Features.Admin.Settings.GetSetting;

public record GetSettingQuery(string Key) : IRequest<string?>;
