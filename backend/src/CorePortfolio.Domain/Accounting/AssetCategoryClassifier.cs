using System.Globalization;
using System.Text;

namespace CorePortfolio.Domain.Accounting;

public static class AssetCategoryClassifier
{
    public static bool IsCrypto(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName)) return false;

        var decomposed = categoryName.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        }

        var value = builder.ToString().Normalize(NormalizationForm.FormC);
        return value.Contains("crypto") || value.Contains("tien ma hoa") || value.Contains("tien dien tu");
    }
}
