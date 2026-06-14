using MediatR;

namespace CorePortfolio.API.Features.Reports.TakeDailySnapshot;

public record TakeDailySnapshotCommand() : IRequest<bool>;
