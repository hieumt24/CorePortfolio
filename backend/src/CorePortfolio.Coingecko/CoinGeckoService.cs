using System.Text.Json;
using System.Text.RegularExpressions;
using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CorePortfolio.Coingecko;

public class CoinGeckoService : ICryptoPriceService, IPriceProvider
{
    private static readonly SemaphoreSlim RequestLock = new(1, 1);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public CoinGeckoService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = cache;
    }

    public string Source => "CoinGecko";

    public async Task<PriceQuote?> GetQuoteAsync(string symbolOrExternalId, string currency, CancellationToken cancellationToken = default)
    {
        var price = await GetPriceAsync(symbolOrExternalId, cancellationToken);
        return price is null ? null : new PriceQuote(price.Value, currency.ToUpperInvariant(), Source, DateTime.UtcNow, "Fresh");
    }

    public async Task<decimal?> GetPriceAsync(string coinId, CancellationToken cancellationToken = default)
    {
        var prices = await GetPricesAsync([coinId], cancellationToken);
        return prices.TryGetValue(coinId, out var price) ? price : null;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetPricesAsync(
        IEnumerable<string> coinIds,
        CancellationToken cancellationToken = default)
    {
        var ids = coinIds
            .Where(id => !string.IsNullOrWhiteSpace(id) && Regex.IsMatch(id, @"^[a-z0-9\-]+$"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        AddCachedPrices(ids, prices);
        var missingIds = ids.Where(id => !prices.ContainsKey(id)).ToArray();
        if (missingIds.Length == 0)
            return prices;

        await RequestLock.WaitAsync(cancellationToken);
        try
        {
            AddCachedPrices(missingIds, prices);
            missingIds = missingIds.Where(id => !prices.ContainsKey(id)).ToArray();
            if (missingIds.Length == 0)
                return prices;

            var client = _httpClientFactory.CreateClient("CoinGecko");
            var idsParameter = Uri.EscapeDataString(string.Join(',', missingIds));
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.coingecko.com/api/v3/simple/price?vs_currencies=usd&ids={idsParameter}");
            var apiKey = _configuration["CoinGecko:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("x-cg-demo-api-key", apiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return prices;

            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, default, cancellationToken);
            var cacheSeconds = Math.Clamp(_configuration.GetValue("CoinGecko:CacheSeconds", 300), 20, 3600);
            foreach (var coinId in missingIds)
            {
                if (document.RootElement.TryGetProperty(coinId, out var coinElement)
                    && coinElement.TryGetProperty("usd", out var usdElement)
                    && usdElement.TryGetDecimal(out var price)
                    && price > 0)
                {
                    prices[coinId] = price;
                    _cache.Set(CacheKey(coinId), price, TimeSpan.FromSeconds(cacheSeconds));
                }
            }

            return prices;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return prices;
        }
        finally
        {
            RequestLock.Release();
        }
    }

    private void AddCachedPrices(IEnumerable<string> coinIds, IDictionary<string, decimal> prices)
    {
        foreach (var coinId in coinIds)
            if (_cache.TryGetValue<decimal>(CacheKey(coinId), out var price))
                prices[coinId] = price;
    }

    private static string CacheKey(string coinId) => $"coingecko:usd:{coinId.ToLowerInvariant()}";
}
