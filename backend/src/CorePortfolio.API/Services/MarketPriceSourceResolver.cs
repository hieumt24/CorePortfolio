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
        ["CMC20"] = "coinmarketcap-20-index-dtf",
        ["HYPE"] = "hyperliquid",
        ["LINK"] = "chainlink",
        ["NEAR"] = "near",
        ["NIGHT"] = "midnight-3",
        ["SOL"] = "solana"
    };

    public static bool Normalize(MarketAsset asset)
    {
        if (asset.PriceSource.Equals("DNSE", StringComparison.OrdinalIgnoreCase))
        {
            asset.PriceSource = "KBS";
            asset.ExternalId = string.IsNullOrWhiteSpace(asset.ExternalId)
                ? asset.Symbol.ToUpperInvariant()
                : asset.ExternalId.ToUpperInvariant();
            asset.PriceStatus = "Stale";
            asset.LastPriceError = null;
            return true;
        }

        if (asset.PriceSource.Equals("KBS", StringComparison.OrdinalIgnoreCase))
        {
            var externalId = string.IsNullOrWhiteSpace(asset.ExternalId)
                ? asset.Symbol.ToUpperInvariant()
                : asset.ExternalId.ToUpperInvariant();
            if (asset.PriceSource == "KBS" && asset.ExternalId == externalId)
                return false;
            asset.PriceSource = "KBS";
            asset.ExternalId = externalId;
            return true;
        }

        if (asset.PriceSource.Equals("CoinGecko", StringComparison.OrdinalIgnoreCase))
        {
            if (CoinGeckoIds.TryGetValue(asset.Symbol, out var mappedId) && !string.Equals(asset.ExternalId, mappedId, StringComparison.OrdinalIgnoreCase))
            {
                asset.ExternalId = mappedId;
                asset.PriceStatus = "Stale";
                asset.LastPriceError = null;
                return true;
            }
            return false;
        }

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
            asset.PriceSource = "KBS";
            asset.ExternalId = asset.Symbol.ToUpperInvariant();
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
