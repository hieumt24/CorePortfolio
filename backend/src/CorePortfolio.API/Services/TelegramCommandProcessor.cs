using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Domain.Models.Telegram;
using CorePortfolio.Infrastructure.Data;
using CorePortfolio.API.Features.Cashflows.CreateCashflowRecord;
using CorePortfolio.API.Features.Transactions.CreateTransaction;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.Domain.Entities;

namespace CorePortfolio.API.Services;

public class TelegramCommandProcessor : ITelegramCommandProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly IMediator _mediator;

    public TelegramCommandProcessor(AppDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<string> ProcessCashflowAsync(CashflowCommandData data, CancellationToken cancellationToken = default)
    {
        var adminUser = await _dbContext.Users
            .Where(user => user.Role == "Admin" && user.IsActive)
            .OrderBy(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (adminUser == null)
        {
            return "❌ Lỗi: Không tìm thấy người dùng Admin trong hệ thống.";
        }

        var portfolio = await _dbContext.Portfolios
            .Where(item => item.UserId == adminUser.Id)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (portfolio == null)
        {
            return "❌ Lỗi: Bạn chưa tạo Portfolio nào. Vui lòng tạo trên ứng dụng web trước.";
        }

        var categories = await _dbContext.CashflowCategories
            .Where(c => c.IsGlobal || c.UserId == adminUser.Id)
            .ToListAsync(cancellationToken);

        var category = categories.FirstOrDefault(c =>
            c.Name.Trim().Equals(data.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (category == null)
        {
            var catNames = string.Join(", ", categories.Select(c => c.Name));
            return $"❌ Lỗi: Không tìm thấy danh mục `{data.CategoryName}`.\nCác danh mục hiện có: {catNames}";
        }

        if (data.ExpenseOnly && category.Type != CashflowType.Expense)
        {
            return $"❌ Danh mục `{category.Name}` không phải danh mục chi tiêu.";
        }

        var command = new CreateCashflowRecordForUserCommand(
            UserId: adminUser.Id,
            PortfolioId: portfolio.Id,
            CategoryId: category.Id,
            Amount: data.Amount,
            Currency: "VND",
            Date: data.Date,
            Description: data.Description
        );

        await _mediator.Send(command, cancellationToken);
        
        var dateStr = data.Date.ToString("dd/MM/yyyy");
        return $"✅ Đã lưu *{data.Amount:N0} VND* vào *{category.Name}* trên portfolio *{portfolio.Name}* ngày {dateStr}.\nĐã tạo đồng thời Cashflow và Transaction rút tiền.";
    }

    public async Task<string> ProcessTransactionAsync(TransactionCommandData data, CancellationToken cancellationToken = default)
    {
        var adminUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Admin", cancellationToken);
        if (adminUser == null)
        {
            return "❌ Lỗi: Không tìm thấy người dùng Admin trong hệ thống.";
        }

        var portfolio = await _dbContext.Portfolios.FirstOrDefaultAsync(p => p.UserId == adminUser.Id, cancellationToken);
        if (portfolio == null)
        {
            return "❌ Lỗi: Bạn chưa tạo Portfolio nào. Vui lòng tạo trên ứng dụng web trước.";
        }

        var asset = await _dbContext.MarketAssets.FirstOrDefaultAsync(a => a.Symbol == data.Symbol, cancellationToken);
        if (asset == null)
        {
            return $"❌ Lỗi: Mã `{data.Symbol}` chưa có trong Market Assets. Vui lòng tạo mã này trước trên Web.";
        }

        var command = new CreateTransactionCommand(
            PortfolioId: portfolio.Id,
            AssetId: asset.Id,
            Type: (TransactionType)data.Type,
            Quantity: data.Quantity,
            Price: data.Price,
            Currency: "VND",
            Timestamp: data.Date
        );

        await _mediator.Send(command, cancellationToken);

        var actionStr = data.Type == 1 ? "Mua" : "Bán";
        var dateStr = data.Date.ToLocalTime().ToString("dd/MM/yyyy");
        return $"✅ Đã *{actionStr} {data.Quantity:N0}* mã *{asset.Symbol}* với giá *{data.Price:N0} VND* ngày {dateStr}.";
    }
}
