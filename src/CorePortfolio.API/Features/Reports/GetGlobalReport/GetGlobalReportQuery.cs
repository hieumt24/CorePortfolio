using MediatR;

namespace CorePortfolio.API.Features.Reports.GetGlobalReport;

public record GlobalReportDto(
    List<CategoryAllocationDto> AllocationsByCategory,
    List<PortfolioAllocationDto> AllocationsByPortfolio
);

public record CategoryAllocationDto(
    string CategoryName,
    string Currency,
    decimal TotalInvested,
    decimal CurrentValue
);

public record PortfolioCurrencyAllocationDto(
    string Currency,
    decimal TotalInvested,
    decimal CurrentValue
);

public record PortfolioAllocationDto(
    Guid PortfolioId,
    string PortfolioName,
    List<PortfolioCurrencyAllocationDto> Currencies
);

public record GetGlobalReportQuery() : IRequest<GlobalReportDto>;
