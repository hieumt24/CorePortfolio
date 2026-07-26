using System.Text.Json;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CorePortfolio.KBS;

public sealed class KbsStockInstrumentService : IStockInstrumentService
{
    private const string CacheKey = "kbs:instruments";
    private static readonly SemaphoreSlim RequestLock = new(1, 1);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KbsStockInstrumentService> _logger;

    public KbsStockInstrumentService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<KbsStockInstrumentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<StockInstrument>> SearchInstrumentsAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var instruments = await GetInstrumentsAsync(cancellationToken);
        var normalizedQuery = query.Trim();
        return instruments
            .Where(instrument =>
                string.IsNullOrEmpty(normalizedQuery)
                || instrument.Symbol.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || instrument.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(instrument =>
                instrument.Symbol.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .ThenBy(instrument => instrument.Symbol)
            .Take(Math.Clamp(limit, 1, 50))
            .ToArray();
    }

    private async Task<IReadOnlyList<StockInstrument>> GetInstrumentsAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue<IReadOnlyList<StockInstrument>>(CacheKey, out var cached) && cached is not null)
            return cached;

        await RequestLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue<IReadOnlyList<StockInstrument>>(CacheKey, out cached) && cached is not null)
                return cached;

            var client = _httpClientFactory.CreateClient(KbsConfiguration.HttpClientName);
            using var response = await client.GetAsync("stock/search/data", cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, default, cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var instruments = document.RootElement
                .EnumerateArray()
                .Where(item =>
                    !item.TryGetProperty("type", out var type)
                    || type.GetString() is "stock" or "etf")
                .Select(MapInstrument)
                .Where(instrument => !string.IsNullOrWhiteSpace(instrument.Symbol))
                .ToArray();

            _cache.Set(
                CacheKey,
                instruments,
                TimeSpan.FromHours(KbsConfiguration.GetInstrumentCacheHours(_configuration)));
            return instruments;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load the KBS instrument catalog.");
            return [];
        }
        finally
        {
            RequestLock.Release();
        }
    }

    private static StockInstrument MapInstrument(JsonElement item)
    {
        var symbol = GetString(item, "symbol");
        var name = GetString(item, "name");
        var nameEn = GetString(item, "nameEn");
        return new StockInstrument
        {
            Symbol = symbol,
            MarketId = GetString(item, "exchange"),
            SecurityGroupId = GetString(item, "type"),
            ShortName = string.IsNullOrWhiteSpace(name) ? nameEn : name,
            Name = string.IsNullOrWhiteSpace(name) ? nameEn : name
        };
    }

    private static string GetString(JsonElement item, string propertyName) =>
        item.TryGetProperty(propertyName, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
