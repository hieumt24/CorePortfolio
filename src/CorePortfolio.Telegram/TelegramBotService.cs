using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.Telegram;

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
            await HandleCommand(botClient, senderChatId, reportService => reportService.GetGlobalReportMarkdownAsync(cancellationToken), cancellationToken);
        }
        else if (messageText == "/portfolio")
        {
            await HandleCommand(botClient, senderChatId, reportService => reportService.GetPortfoliosListMarkdownAsync(cancellationToken), cancellationToken);
        }
        else if (messageText == "/balance")
        {
            await HandleCommand(botClient, senderChatId, reportService => reportService.GetBalanceMarkdownAsync(cancellationToken), cancellationToken);
        }
        else if (messageText == "/start")
        {
            await botClient.SendMessage(
                chatId: senderChatId,
                text: "Chào mừng! Các lệnh hỗ trợ:\n- `/report`: Báo cáo chi tiết\n- `/portfolio`: Danh sách danh mục\n- `/balance`: Tổng số dư",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleCommand(ITelegramBotClient botClient, string chatId, Func<IPortfolioReportService, Task<string>> action, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var reportService = scope.ServiceProvider.GetRequiredService<IPortfolioReportService>();

            var text = await action(reportService);

            await botClient.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling command in Telegram Bot.");
            await botClient.SendMessage(chatId, "Đã xảy ra lỗi khi thực thi lệnh.", cancellationToken: cancellationToken);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram Bot Error");
        return Task.CompletedTask;
    }
}
