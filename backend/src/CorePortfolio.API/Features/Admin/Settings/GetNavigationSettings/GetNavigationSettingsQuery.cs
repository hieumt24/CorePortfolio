using MediatR;

namespace CorePortfolio.API.Features.Admin.Settings.GetNavigationSettings;

public record NavigationFeatureDto(string Key, bool IsEnabled);

public record GetNavigationSettingsQuery : IRequest<IReadOnlyList<NavigationFeatureDto>>;
