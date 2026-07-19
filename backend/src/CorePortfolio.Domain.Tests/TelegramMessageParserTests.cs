using CorePortfolio.Telegram;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public class TelegramMessageParserTests
{
    [Fact]
    public void ParseCashflow_ChiCommand_ParsesExpenseWithShortAmount()
    {
        var result = TelegramMessageParser.ParseCashflow("/chi 50k \"Ăn uống\" \"Ăn sáng\"");

        Assert.NotNull(result);
        Assert.Equal(50_000m, result.Amount);
        Assert.Equal("Ăn uống", result.CategoryName);
        Assert.Equal("Ăn sáng", result.Description);
        Assert.True(result.ExpenseOnly);
    }

    [Fact]
    public void ParseCashflow_GroupCommand_ParsesVietnameseThousandsAndDate()
    {
        var result = TelegramMessageParser.ParseCashflow(
            "/chi@CorePortfolioBot 50.000 \"Ăn uống\" \"Ăn trưa\" 19/07/2026");

        Assert.NotNull(result);
        Assert.Equal(50_000m, result.Amount);
        Assert.Equal(new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc), result.Date);
    }

    [Fact]
    public void ParseCashflow_CfCommand_RemainsBackwardCompatible()
    {
        var result = TelegramMessageParser.ParseCashflow(
            "/cf 1.5m \"Lương\" \"Lương tháng 7\" 2026-07-19");

        Assert.NotNull(result);
        Assert.Equal(1_500_000m, result.Amount);
        Assert.False(result.ExpenseOnly);
    }

    [Fact]
    public void ParseCashflow_InvalidDate_ReturnsNull()
    {
        Assert.Null(TelegramMessageParser.ParseCashflow(
            "/chi 50k \"Ăn uống\" \"Ăn sáng\" 19-99-2026"));
    }
}
