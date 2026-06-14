using MediatR;

namespace CorePortfolio.API.Features.Reports.GetGlobalHistory;

public record SnapshotDto(string Date, decimal TotalInvested, decimal TotalValue);

public record GetGlobalHistoryQuery() : IRequest<List<SnapshotDto>>;
