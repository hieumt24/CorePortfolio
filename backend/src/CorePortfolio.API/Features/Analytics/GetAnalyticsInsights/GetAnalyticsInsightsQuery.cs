using CorePortfolio.API.Features.Analytics.GetAnalyticsOverview;
using MediatR;

namespace CorePortfolio.API.Features.Analytics.GetAnalyticsInsights;

public sealed record GetAnalyticsInsightsQuery(
    Guid? PortfolioId,
    DateTime? From,
    DateTime? To,
    string Currency) : IRequest<AnalyticsInsightsDto>;

public sealed class GetAnalyticsInsightsHandler(IMediator mediator)
    : IRequestHandler<GetAnalyticsInsightsQuery, AnalyticsInsightsDto>
{
    public async Task<AnalyticsInsightsDto> Handle(
        GetAnalyticsInsightsQuery request,
        CancellationToken cancellationToken)
    {
        var overview = await mediator.Send(
            new GetAnalyticsOverviewQuery(
                request.PortfolioId,
                request.From,
                request.To,
                request.Currency),
            cancellationToken);
        return overview.Insights;
    }
}
