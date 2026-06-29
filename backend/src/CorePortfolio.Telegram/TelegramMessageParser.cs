using System.Globalization;
using System.Text.RegularExpressions;
using CorePortfolio.Domain.Models.Telegram;

namespace CorePortfolio.Telegram;

public static class TelegramMessageParser
{
    public static CashflowCommandData? ParseCashflow(string text)
    {
        // Format: /cf 50k "Ăn uống" "Ăn sáng" 2023-10-15
        var match = Regex.Match(text, @"^/cf\s+([0-9.,]+[kmKM]?)\s+""([^""]+)""\s+""([^""]+)""(?:\s+(.+))?$");
        if (!match.Success) return null;

        var amountStr = match.Groups[1].Value;
        var category = match.Groups[2].Value;
        var description = match.Groups[3].Value;
        var dateStr = match.Groups[4].Value;

        if (!TryParseAmount(amountStr, out var amount)) return null;

        DateTime date = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dateStr))
        {
            if (DateTime.TryParse(dateStr, out var parsedDate))
            {
                date = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
        }

        return new CashflowCommandData
        {
            Amount = amount,
            CategoryName = category,
            Description = description,
            Date = date
        };
    }

    public static TransactionCommandData? ParseTransaction(string text)
    {
        // Format: /tx buy HPG 100 25000 2023-10-15
        var match = Regex.Match(text, @"^/tx\s+(buy|sell|mua|bán)\s+([A-Za-z0-9]+)\s+([0-9.,]+[kmKM]?)\s+([0-9.,]+[kmKM]?)(?:\s+(.+))?$", RegexOptions.IgnoreCase);
        if (!match.Success) return null;

        var actionStr = match.Groups[1].Value.ToLower();
        var symbol = match.Groups[2].Value.ToUpper();
        var quantityStr = match.Groups[3].Value;
        var priceStr = match.Groups[4].Value;
        var dateStr = match.Groups[5].Value;

        int type = (actionStr == "buy" || actionStr == "mua") ? 1 : 2; // Assuming 1 = Buy, 2 = Sell based on Domain enums

        if (!TryParseAmount(quantityStr, out var quantity) || !TryParseAmount(priceStr, out var price))
            return null;

        DateTime date = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dateStr))
        {
            if (DateTime.TryParse(dateStr, out var parsedDate))
            {
                date = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
        }

        return new TransactionCommandData
        {
            Type = type,
            Symbol = symbol,
            Quantity = quantity,
            Price = price,
            Date = date
        };
    }

    private static bool TryParseAmount(string input, out decimal result)
    {
        result = 0;
        input = input.ToLower();
        decimal multiplier = 1;

        if (input.EndsWith("k"))
        {
            multiplier = 1000;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith("m"))
        {
            multiplier = 1000000;
            input = input.Substring(0, input.Length - 1);
        }

        if (decimal.TryParse(input.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            result = parsed * multiplier;
            return true;
        }

        return false;
    }
}
