namespace CorePortfolio.Domain.Interfaces;

public interface IPortfolioReportService
{
    Task<string> GetGlobalReportMarkdownAsync(CancellationToken cancellationToken = default);
    Task<string> GetPortfoliosListMarkdownAsync(CancellationToken cancellationToken = default);
    Task<string> GetBalanceMarkdownAsync(CancellationToken cancellationToken = default);
}
