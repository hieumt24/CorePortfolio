using System.Text;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.API.Features.Reports.GetGlobalReport;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Services;

public class PortfolioReportService : IPortfolioReportService
{
    private readonly IServiceProvider _serviceProvider;

    public PortfolioReportService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<string> GetGlobalReportMarkdownAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Admin", cancellationToken);
        if (adminUser == null)
        {
            return "Lỗi: Không tìm thấy người dùng Admin trong hệ thống.";
        }

        var report = await mediator.Send(new GetGlobalReportQuery(adminUser.Id), cancellationToken);
        return FormatReport(report);
    }

    public async Task<string> GetPortfoliosListMarkdownAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Admin", cancellationToken);
        if (adminUser == null) return "Lỗi: Không tìm thấy người dùng Admin.";

        var portfolios = await dbContext.Portfolios
            .AsNoTracking()
            .Where(p => p.UserId == adminUser.Id)
            .ToListAsync(cancellationToken);

        if (portfolios.Count == 0) return "Bạn chưa có danh mục nào.";

        var sb = new StringBuilder();
        sb.AppendLine("📋 *DANH SÁCH DANH MỤC (PORTFOLIO)*");
        sb.AppendLine();
        
        for (int i = 0; i < portfolios.Count; i++)
        {
            var p = portfolios[i];
            sb.AppendLine($"{i + 1}. *{p.Name}*");
            if (!string.IsNullOrWhiteSpace(p.Description))
            {
                sb.AppendLine($"   _Mô tả: {p.Description}_");
            }
        }

        return sb.ToString();
    }

    public async Task<string> GetBalanceMarkdownAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Admin", cancellationToken);
        if (adminUser == null) return "Lỗi: Không tìm thấy người dùng Admin.";

        var report = await mediator.Send(new GetGlobalReportQuery(adminUser.Id), cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("💰 *TỔNG SỐ DƯ (BALANCE)*");
        sb.AppendLine();

        if (report.AllocationsByCategory.Count == 0)
        {
            return "Chưa có dữ liệu đầu tư.";
        }

        // Aggregate by currency
        var totalsByCurrency = report.AllocationsByCategory
            .GroupBy(c => c.Currency)
            .Select(g => new
            {
                Currency = g.Key,
                TotalInvested = g.Sum(x => x.TotalInvested),
                CurrentValue = g.Sum(x => x.CurrentValue)
            }).ToList();

        foreach (var total in totalsByCurrency)
        {
            var profit = total.CurrentValue - total.TotalInvested;
            var profitPercent = total.TotalInvested > 0 ? (profit / total.TotalInvested) * 100 : 0;
            var emoji = profit >= 0 ? "🟢" : "🔴";

            sb.AppendLine($"*Tiền tệ: {total.Currency}*");
            sb.AppendLine($"- Tổng vốn: {total.TotalInvested:N0}");
            sb.AppendLine($"- Hiện tại: {total.CurrentValue:N0}");
            sb.AppendLine($"- Lợi nhuận: {emoji} {profit:N0} ({profitPercent:N2}%)");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string FormatReport(GlobalReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📊 *BÁO CÁO COREPORTFOLIO*");
        sb.AppendLine();

        if (report.AllocationsByPortfolio.Count == 0)
        {
            return "Chưa có dữ liệu danh mục.";
        }

        foreach (var portfolio in report.AllocationsByPortfolio)
        {
            sb.AppendLine($"💼 *Danh mục: {portfolio.PortfolioName}*");
            foreach (var curr in portfolio.Currencies)
            {
                var profit = curr.CurrentValue - curr.TotalInvested;
                var profitPercent = curr.TotalInvested > 0 ? (profit / curr.TotalInvested) * 100 : 0;
                var emoji = profit >= 0 ? "🟢" : "🔴";
                
                sb.AppendLine($"  - Tiền tệ: {curr.Currency}");
                sb.AppendLine($"  - Vốn: {curr.TotalInvested:N0}");
                sb.AppendLine($"  - Hiện tại: {curr.CurrentValue:N0}");
                sb.AppendLine($"  - Lợi nhuận: {emoji} {profit:N0} ({profitPercent:N2}%)");
            }
            sb.AppendLine();
        }

        sb.AppendLine("📈 *Phân bổ theo danh mục tài sản*");
        foreach (var cat in report.AllocationsByCategory)
        {
            var profit = cat.CurrentValue - cat.TotalInvested;
            var profitPercent = cat.TotalInvested > 0 ? (profit / cat.TotalInvested) * 100 : 0;
            var emoji = profit >= 0 ? "🟢" : "🔴";
            sb.AppendLine($"- *{cat.CategoryName}*: Vốn {cat.TotalInvested:N0} {cat.Currency} | Hiện tại {cat.CurrentValue:N0} | {emoji} {profitPercent:N2}%");
        }

        return sb.ToString();
    }
}
