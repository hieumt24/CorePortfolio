using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CorePortfolio.Fmarket;

public sealed class FmarketFundNavService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IConfiguration configuration) : IFundNavService
{
    private const string CacheKey = "fmarket:funds";

    public async Task<IReadOnlyList<FundNavInstrument>> GetFundsAsync(
        CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<FundNavInstrument>? cached) && cached is not null)
            return cached;

        var client = httpClientFactory.CreateClient(FmarketConfiguration.HttpClientName);
        using var response = await client.PostAsJsonAsync("res/products/filter", new
        {
            types = new[] { "NEW_FUND", "TRADING_FUND" },
            issuerIds = Array.Empty<int>(),
            sortOrder = "DESC",
            sortField = "navTo6Months",
            page = 1,
            pageSize = 500,
            isIpo = false,
            fundAssetTypes = Array.Empty<int>(),
            bondRemainPeriods = Array.Empty<int>(),
            searchField = "",
            isBuyByReward = false,
            thirdAppIds = Array.Empty<int>()
        }, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var rows = FindRows(document.RootElement);
        if (rows.ValueKind != JsonValueKind.Array)
            throw new HttpRequestException("Phản hồi Fmarket không chứa danh sách chứng chỉ quỹ hợp lệ.");
        var funds = rows
            .EnumerateArray()
            .Select(ParseFund)
            .Where(item => item is not null)
            .Select(item => item!)
            .GroupBy(item => item.ExternalId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (funds.Length == 0)
            throw new HttpRequestException("Fmarket không trả về danh sách chứng chỉ quỹ.");

        cache.Set(CacheKey, funds, TimeSpan.FromMinutes(FmarketConfiguration.GetCacheMinutes(configuration)));
        return funds;
    }

    private static JsonElement FindRows(JsonElement root)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals("rows", StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.Array)
                return property.Value;
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                var nested = FindRows(property.Value);
                if (nested.ValueKind == JsonValueKind.Array)
                    return nested;
            }
        }
        return default;
    }

    private static FundNavInstrument? ParseFund(JsonElement row)
    {
        var id = GetString(row, "id");
        var symbol = GetString(row, "shortName");
        var name = GetString(row, "name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(symbol))
            return null;

        var nav = GetDecimal(row, "nav");
        var fundType = row.TryGetProperty("dataFundAssetType", out var type)
            ? GetString(type, "name")
            : null;
        var asOf = DateTime.UtcNow;
        if (row.TryGetProperty("productNavChange", out var navChange))
        {
            var unixMilliseconds = GetLong(navChange, "updateAt");
            if (unixMilliseconds > 0)
                asOf = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).UtcDateTime;
        }

        return new FundNavInstrument(
            id,
            symbol.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(name) ? symbol.Trim() : name.Trim(),
            fundType,
            nav,
            asOf);
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static decimal GetDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value)
        && decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0m;

    private static long GetLong(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value)
        && long.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0L;
}
