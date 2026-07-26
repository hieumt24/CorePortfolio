using MediatR;

namespace CorePortfolio.API.Features.Reports.GetGlobalHistory;

public record SnapshotDto(
    string Date,
    decimal TotalInvested,
    decimal TotalValue,
    decimal HoldingsValue,
    decimal CashValue,
    decimal NetAssetValue,
    decimal NetExternalFlow,
    decimal RealizedPnl,
    decimal UnrealizedPnl,
    decimal Income,
    decimal Fees,
    string Currency,
    decimal UsdToVndRate,
    DateTime ValuationTimestamp,
    string QualityStatus,
    int StaleAssetCount,
    int UnclassifiedCashFlowCount);

public record GetGlobalHistoryQuery() : IRequest<List<SnapshotDto>>;
