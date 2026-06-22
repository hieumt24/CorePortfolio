using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CorePortfolio.API.Services;

public class TelegramCronService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelegramCronService> _logger;
    private readonly TimeZoneInfo _vietnamTimeZone;

    public TelegramCronService(IServiceProvider serviceProvider, ILogger<TelegramCronService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        try
        {
            _vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Windows
        }
        catch (TimeZoneNotFoundException)
        {
            _vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); // Linux/Mac
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Cron Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vietnamTimeZone);
                
                var next10AM = new DateTime(now.Year, now.Month, now.Day, 10, 0, 0);
                if (now > next10AM) next10AM = next10AM.AddDays(1);

                var next8PM = new DateTime(now.Year, now.Month, now.Day, 20, 0, 0);
                if (now > next8PM) next8PM = next8PM.AddDays(1);

                var nextRunTime = next10AM < next8PM ? next10AM : next8PM;
                var isMorningReport = next10AM < next8PM;

                var delay = nextRunTime - now;
                _logger.LogInformation("Next Telegram report scheduled at {NextRunTime} (in {DelayHours}h {DelayMinutes}m). IsMorning: {IsMorning}", 
                    nextRunTime, delay.Hours, delay.Minutes, isMorningReport);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Triggering Telegram Report...");
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var reportService = scope.ServiceProvider.GetRequiredService<IPortfolioReportService>();

                    // For now, we will send reports to all users who have a registered Telegram ChatId.
                    // This requires User entity to have TelegramChatId. Let's assume we can fetch users with portfolios.
                    var users = await dbContext.Users.Where(u => !string.IsNullOrEmpty(u.PasswordHash)).ToListAsync(stoppingToken); // Hack: get all users for now

                    foreach (var user in users)
                    {
                        try
                        {
                            if (isMorningReport)
                            {
                                // 10:00 AM Portfolio Summary
                                var summary = await reportService.GetGlobalReportMarkdownAsync(stoppingToken);
                                if (!string.IsNullOrEmpty(summary))
                                {
                                    // Normally you'd store the ChatId in User entity. Since we might not have it, we just broadcast to the bot's known chats or we skip if no ChatId.
                                    // For demonstration, if we had user.TelegramChatId:
                                    // await telegramBot.SendMessageAsync(user.TelegramChatId, summary);
                                    
                                    // As a fallback, maybe just broadcast to a default admin group or log it.
                                    _logger.LogInformation("Generated Morning Report for User {Username}:\n{Summary}", user.Username, summary);
                                }
                            }
                            else
                            {
                                // 20:00 PM Cashflow Reminder
                                var reminderMsg = $"🔔 Xin chào {user.Username},\n\nBạn đã ghi chép các khoản thu/chi trong ngày hôm nay chưa? Đừng quên cập nhật để quản lý tài chính hiệu quả nhé!";
                                _logger.LogInformation("Generated Evening Reminder for User {Username}", user.Username);
                                // await telegramBot.SendMessageAsync(user.TelegramChatId, reminderMsg);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating report for user {UserId}", user.Id);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Telegram Cron Service loop");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait a bit before retrying on error
            }
        }
    }
}
