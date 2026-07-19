using System.Globalization;
using System.Text.RegularExpressions;
using CorePortfolio.Domain.Models.Telegram;

namespace CorePortfolio.Telegram;

public static class TelegramMessageParser
{
    public static CashflowCommandData? ParseCashflow(string text)
    {
        // /chi 50k "Ăn uống" "Ăn sáng" 2026-07-19
        // /cf  50k "Ăn uống" "Ăn sáng" 2026-07-19
        var match = Regex.Match(text,
            @"^/(chi|cf)(?:@[A-Za-z0-9_]+)?\s+([0-9.,]+[kmKM]?)\s+""([^""]+)""\s+""([^""]+)""(?:\s+(\S+))?$",
            RegexOptions.IgnoreCase);
        if (!match.Success) return null;

        if (!TryParseAmount(match.Groups[2].Value, out var amount) || amount <= 0)
            return null;

        var date = DateTime.UtcNow;
        var dateText = match.Groups[5].Value;
        if (!string.IsNullOrWhiteSpace(dateText))
        {
            var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy" };
            if (!DateTime.TryParseExact(dateText, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedDate))
                return null;
            date = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        }

        return new CashflowCommandData
        {
            Amount = amount,
            CategoryName = match.Groups[3].Value.Trim(),
            Description = match.Groups[4].Value.Trim(),
            Date = date,
            ExpenseOnly = match.Groups[1].Value.Equals("chi", StringComparison.OrdinalIgnoreCase)
        };
    }

    public static TransactionCommandData? ParseTransaction(string text)
    {
        var match = Regex.Match(text,
            @"^/tx(?:@[A-Za-z0-9_]+)?\s+(buy|sell|mua|bán)\s+([A-Za-z0-9]+)\s+([0-9.,]+[kmKM]?)\s+([0-9.,]+[kmKM]?)(?:\s+(\S+))?$",
            RegexOptions.IgnoreCase);
        if (!match.Success) return null;

        var action = match.Groups[1].Value.ToLowerInvariant();
        if (!TryParseAmount(match.Groups[3].Value, out var quantity)
            || !TryParseAmount(match.Groups[4].Value, out var price)
            || quantity <= 0 || price < 0)
            return null;

        var date = DateTime.UtcNow;
        var dateText = match.Groups[5].Value;
        if (!string.IsNullOrWhiteSpace(dateText))
        {
            var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy" };
            if (!DateTime.TryParseExact(dateText, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedDate))
                return null;
            date = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        }

        return new TransactionCommandData
        {
            Type = action is "buy" or "mua" ? 1 : 2,
            Symbol = match.Groups[2].Value.ToUpperInvariant(),
            Quantity = quantity,
            Price = price,
            Date = date
        };
    }

    private static bool TryParseAmount(string input, out decimal result)
    {
        result = 0;
        var normalized = input.Trim().ToLowerInvariant();
        decimal multiplier = 1;

        if (normalized.EndsWith('k'))
        {
            multiplier = 1_000;
            normalized = normalized[..^1];
        }
        else if (normalized.EndsWith('m'))
        {
            multiplier = 1_000_000;
            normalized = normalized[..^1];
        }

        if (multiplier == 1 && Regex.IsMatch(normalized, @"^\d{1,3}([.,]\d{3})+$"))
            normalized = normalized.Replace(",", "").Replace(".", "");
        else
            normalized = normalized.Replace(",", ".");

        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return false;

        result = parsed * multiplier;
        return true;
    }
}
