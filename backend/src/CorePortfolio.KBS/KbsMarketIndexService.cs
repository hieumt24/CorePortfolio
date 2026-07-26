using System.Globalization;
using System.Text.Json;
using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CorePortfolio.KBS;

public sealed class KbsMarketIndexService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<KbsMarketIndexService> logger) : IMarketIndexService
{
    private static readonly HashSet<string> SupportedSymbols =
        new(["VNINDEX", "VN30"], StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public async Task<IReadOnlyList<MarketIndexQuote>> GetQuotesAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbols = symbols
            .Select(symbol => symbol.Trim().ToUpperInvariant())
            .Where(SupportedSymbols.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var results = await Task.WhenAll(normalizedSymbols.Select(
            symbol => GetQuoteAsync(symbol, cancellationToken)));
        return results;
    }

    private async Task<MarketIndexQuote> GetQuoteAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"kbs:index:{symbol}";
        var lastKnownCacheKey = $"{cacheKey}:last-known";
        if (cache.TryGetValue<MarketIndexQuote>(cacheKey, out var cached) && cached is not null)
            return cached;

        try
        {
            var end = DateTime.UtcNow.Date;
            var start = end.AddDays(-KbsConfiguration.GetLookbackDays(configuration));
            var path = $"index/{Uri.EscapeDataString(symbol)}/data_day" +
                       $"?sdate={start:dd-MM-yyyy}&edate={end:dd-MM-yyyy}";
            var client = httpClientFactory.CreateClient(KbsConfiguration.HttpClientName);
            using var response = await client.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, default, cancellationToken);
            var points = ReadLatestPoints(document.RootElement);
            if (points.Count == 0)
                throw new HttpRequestException($"KBS không trả về dữ liệu cho chỉ số {symbol}.");

            var latest = points[^1];
            var previousClose = points.Count > 1 ? points[^2].Close : latest.Close;
            var change = latest.Close - previousClose;
            var changePercent = previousClose == 0 ? 0 : change / previousClose * 100;
            var quote = new MarketIndexQuote(
                symbol,
                symbol == "VNINDEX" ? "VN-Index" : "VN30",
                latest.Close,
                change,
                changePercent,
                new DateTimeOffset(
                    DateTime.SpecifyKind(latest.Time, DateTimeKind.Unspecified),
                    VietnamOffset).UtcDateTime,
                "KBS",
                "Fresh");

            cache.Set(
                cacheKey,
                quote,
                TimeSpan.FromSeconds(KbsConfiguration.GetPriceCacheSeconds(configuration)));
            cache.Set(lastKnownCacheKey, quote);
            return quote;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to load KBS market index {Symbol}.", symbol);
            if (cache.TryGetValue<MarketIndexQuote>(lastKnownCacheKey, out var lastKnown) &&
                lastKnown is not null)
            {
                return lastKnown with
                {
                    Status = "Stale",
                    Error = $"KBS tạm thời không cập nhật được {symbol}; đang hiển thị dữ liệu gần nhất."
                };
            }

            return new MarketIndexQuote(
                symbol,
                symbol == "VNINDEX" ? "VN-Index" : "VN30",
                0,
                0,
                0,
                DateTime.UtcNow,
                "KBS",
                "Error",
                "Không thể cập nhật chỉ số lúc này.");
        }
    }

    internal static IReadOnlyList<(DateTime Time, decimal Close)> ReadLatestPoints(JsonElement root)
    {
        if (!root.TryGetProperty("data_day", out var data) || data.ValueKind != JsonValueKind.Array)
            return [];

        return data.EnumerateArray()
            .Select(item =>
            {
                decimal close = 0;
                DateTime time = default;
                var hasClose = item.TryGetProperty("c", out var closeElement)
                               && TryReadDecimal(closeElement, out close)
                               && close > 0;
                var hasTime = item.TryGetProperty("t", out var timeElement)
                              && DateTime.TryParseExact(
                                  timeElement.GetString(),
                                  "yyyy-MM-dd HH:mm",
                                  CultureInfo.InvariantCulture,
                                  DateTimeStyles.None,
                                  out time);
                return (HasValue: hasClose && hasTime, Time: time, Close: close);
            })
            .Where(point => point.HasValue)
            .OrderBy(point => point.Time)
            .Select(point => (point.Time, point.Close))
            .TakeLast(2)
            .ToArray();
    }

    private static bool TryReadDecimal(JsonElement element, out decimal value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(
                element.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value),
            _ => false
        };
    }
}
