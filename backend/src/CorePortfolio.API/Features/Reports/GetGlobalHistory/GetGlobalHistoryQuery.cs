using MediatR;

namespace CorePortfolio.API.Features.Reports.GetGlobalHistory;

public record SnapshotDto(string Date, decimal TotalInvested, decimal TotalValue, string Currency,
    decimal UsdToVndRate, DateTime ValuationTimestamp, string QualityStatus);

public record GetGlobalHistoryQuery() : IRequest<List<SnapshotDto>>;
