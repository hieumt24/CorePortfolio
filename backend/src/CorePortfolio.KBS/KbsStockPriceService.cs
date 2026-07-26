using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CorePortfolio.KBS;

public sealed partial class KbsStockPriceService : IStockPriceService, IPriceProvider
{
    private static readonly SemaphoreSlim RequestLock = new(1, 1);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KbsStockPriceService> _logger;

    public KbsStockPriceService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<KbsStockPriceService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public string Source => "KBS";

    public async Task<PriceQuote?> GetQuoteAsync(
        string symbolOrExternalId,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var price = await GetStockPriceAsync(symbolOrExternalId, cancellationToken);
        return price is null
            ? null
            : new PriceQuote(price.Value, "VND", Source, DateTime.UtcNow, "Fresh");
    }

    public async Task<decimal?> GetStockPriceAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var cacheKey = $"kbs:price:{normalizedSymbol}";
        if (_cache.TryGetValue<decimal>(cacheKey, out var cachedPrice))
            return cachedPrice;

        await RequestLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue<decimal>(cacheKey, out cachedPrice))
                return cachedPrice;

            var end = DateTime.UtcNow.Date;
            var start = end.AddDays(-KbsConfiguration.GetLookbackDays(_configuration));
            var path = $"stocks/{Uri.EscapeDataString(normalizedSymbol)}/data_day" +
                       $"?sdate={start:dd-MM-yyyy}&edate={end:dd-MM-yyyy}";
            var client = _httpClientFactory.CreateClient(KbsConfiguration.HttpClientName);
            using var response = await client.GetAsync(path, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"KBS trả về HTTP {(int)response.StatusCode} ({response.StatusCode}) cho mã {normalizedSymbol}. " +
                    SafeMessage(responseBody),
                    null,
                    response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, default, cancellationToken);
            var latestPrice = ReadLatestClose(document.RootElement);
            if (latestPrice is null or <= 0)
                return null;

            _cache.Set(
                cacheKey,
                latestPrice.Value,
                TimeSpan.FromSeconds(KbsConfiguration.GetPriceCacheSeconds(_configuration)));
            return latestPrice;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            throw new HttpRequestException(
                $"KBS timeout khi lấy giá {normalizedSymbol}; hệ thống giữ lại giá gần nhất.",
                exception,
                HttpStatusCode.GatewayTimeout);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "KBS returned invalid JSON for {Symbol}.", normalizedSymbol);
            throw new HttpRequestException(
                $"KBS trả về dữ liệu không hợp lệ cho mã {normalizedSymbol}.",
                exception,
                HttpStatusCode.BadGateway);
        }
        finally
        {
            RequestLock.Release();
        }
    }

    internal static decimal? ReadLatestClose(JsonElement root)
    {
        if (!root.TryGetProperty("data_day", out var data) || data.ValueKind != JsonValueKind.Array)
            return null;

        DateTime? latestTime = null;
        decimal? latestClose = null;
        foreach (var item in data.EnumerateArray())
        {
            if (!item.TryGetProperty("c", out var closeElement)
                || !closeElement.TryGetDecimal(out var close)
                || close <= 0)
                continue;

            var time = item.TryGetProperty("t", out var timeElement)
                && DateTime.TryParseExact(
                    timeElement.GetString(),
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsedTime)
                    ? parsedTime
                    : DateTime.MinValue;

            if (latestTime is null || time > latestTime)
            {
                latestTime = time;
                latestClose = close;
            }
        }

        // KBS raw OHLC values are already absolute VND (for example 22,400),
        // matching CorePortfolio's persisted stock price unit.
        return latestClose;
    }

    private static string NormalizeSymbol(string symbol)
    {
        var normalized = symbol.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !StockSymbolPattern().IsMatch(normalized))
            throw new ArgumentException("Mã chứng khoán không hợp lệ.", nameof(symbol));
        return normalized;
    }

    private static string SafeMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return "KBS không trả về nội dung lỗi.";
        var trimmed = responseBody.Trim();
        return trimmed.Length <= 300 ? trimmed : $"{trimmed[..300]}...";
    }

    [GeneratedRegex("^[A-Z0-9]{2,16}$", RegexOptions.CultureInvariant)]
    private static partial Regex StockSymbolPattern();
}
