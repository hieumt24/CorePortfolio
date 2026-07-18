using System.Globalization;
using System.Text;
using CorePortfolio.Domain.Entities;

namespace CorePortfolio.API.Services;

public static class MarketPriceSourceResolver
{
    private static readonly IReadOnlyDictionary<string, string> CoinGeckoIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["BTC"] = "bitcoin",
        ["ETH"] = "ethereum",
        ["ADA"] = "cardano",
        ["FET"] = "artificial-superintelligence-alliance",
        ["ALLO"] = "allora",
        ["CMC20"] = "coinmarketcap-20-index"
    };

    public static bool Normalize(MarketAsset asset)
    {
        if (!string.IsNullOrWhiteSpace(asset.PriceSource)) return false;
        var category = NormalizeText(asset.Category?.Name ?? string.Empty);

        if (category.Contains("crypto") || category.Contains("tien ma hoa"))
        {
            asset.PriceSource = "CoinGecko";
            if (string.IsNullOrWhiteSpace(asset.ExternalId) && CoinGeckoIds.TryGetValue(asset.Symbol, out var coinId))
                asset.ExternalId = coinId;
            asset.PriceStatus = string.IsNullOrWhiteSpace(asset.ExternalId) ? "Error" : "Stale";
            asset.LastPriceError = string.IsNullOrWhiteSpace(asset.ExternalId) ? "Thiếu CoinGecko coin ID." : null;
            return true;
        }

        if (category.Contains("chung khoan") || category.Contains("stock") || category.Contains("etf"))
        {
            asset.PriceSource = "DNSE";
            asset.PriceStatus = "Stale";
            asset.LastPriceError = null;
            return true;
        }

        if (category.Contains("chung chi quy") || category.Contains("fund"))
        {
            asset.PriceSource = "Manual";
            asset.PriceStatus = "Manual";
            asset.LastPriceError = null;
            return true;
        }

        asset.PriceSource = "Manual";
        asset.PriceStatus = "Manual";
        asset.LastPriceError = null;
        return true;
    }

    private static string NormalizeText(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character == 'đ' ? 'd' : character));
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
