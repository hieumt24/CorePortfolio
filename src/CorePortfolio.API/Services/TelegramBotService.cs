using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MediatR;
using CorePortfolio.API.Features.Reports.GetGlobalReport;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Services;

public class TelegramBotService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotService> _logger;
    private TelegramBotClient? _botClient;
    private string _allowedChatId = "";

    public TelegramBotService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<TelegramBotService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var token = _configuration["TelegramBot:Token"];
        _allowedChatId = _configuration["TelegramBot:AllowedChatId"] ?? "";

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("TelegramBot:Token is not configured. Telegram Bot service will not start.");
            return;
        }

        _botClient = new TelegramBotClient(token);
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: stoppingToken
        );

        try
        {
            var me = await _botClient.GetMe(stoppingToken);
            _logger.LogInformation("Telegram Bot Service started. Listening as @{BotName}", me.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Telegram Bot. Token might be invalid.");
            return;
        }
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;
        if (message.Text is not { } messageText) return;

        string senderChatId = message.Chat.Id.ToString();

        if (!string.IsNullOrEmpty(_allowedChatId) && senderChatId != _allowedChatId)
        {
            _logger.LogWarning("Unauthorized access attempt from ChatId: {ChatId}", senderChatId);
            return;
        }

        if (messageText == "/report")
        {
            await HandleReportCommand(botClient, senderChatId, cancellationToken);
        }
        else if (messageText == "/start")
        {
            await botClient.SendMessage(
                chatId: senderChatId,
                text: "Chào mừng! Gõ `/report` để xem báo cáo CorePortfolio.",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleReportCommand(ITelegramBotClient botClient, string chatId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Admin", cancellationToken);
            if (adminUser == null)
            {
                await botClient.SendMessage(chatId, "Lỗi: Không tìm thấy người dùng Admin trong hệ thống.", cancellationToken: cancellationToken);
                return;
            }

            var report = await mediator.Send(new GetGlobalReportQuery(adminUser.Id), cancellationToken);
            var text = FormatReport(report);

            await botClient.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /report command.");
            await botClient.SendMessage(chatId, "Đã xảy ra lỗi khi tạo báo cáo.", cancellationToken: cancellationToken);
        }
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

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram Bot Error");
        return Task.CompletedTask;
    }
}
