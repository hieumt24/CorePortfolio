using System.Globalization;
using System.Text;

namespace CorePortfolio.Domain.Accounting;

public static class AssetCategoryClassifier
{
    public static bool IsCrypto(string? categoryName)
    {
        var value = Normalize(categoryName);
        return value.Contains("crypto") || value.Contains("tien ma hoa") || value.Contains("tien dien tu");
    }

    public static bool IsStock(string? categoryName)
    {
        var value = Normalize(categoryName);
        if (IsFundValue(value)) return false;

        return value.Contains("stock") ||
               value.Contains("equity") ||
               value.Contains("co phieu") ||
               value.Contains("chung khoan");
    }

    public static bool IsFund(string? categoryName)
    {
        var value = Normalize(categoryName);
        return IsFundValue(value);
    }

    public static bool IsFiat(string? categoryName)
    {
        var value = Normalize(categoryName);
        return value.Contains("fiat") ||
               value.Contains("cash") ||
               value.Contains("tien mat") ||
               value.Contains("tien phap dinh");
    }

    private static bool IsFundValue(string value) =>
        value.Contains("fund") ||
        value.Contains("ccq") ||
        value.Contains("etf") ||
        value.Contains("quy") ||
        value.Contains("chung chi quy");

    private static string Normalize(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName)) return string.Empty;

        var decomposed = categoryName.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        }

        return builder
            .ToString()
            .Replace('đ', 'd')
            .Normalize(NormalizationForm.FormC);
    }
}
