using System.Net;
using CorePortfolio.Coingecko;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class CoinGeckoServiceTests
{
    [Fact]
    public async Task GetPricesAsync_BatchesCoinIdsAndUsesCache()
    {
        var handler = new StubHandler();
        var client = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CoinGecko:ApiKey"] = "demo-key",
                ["CoinGecko:CacheSeconds"] = "300"
            })
            .Build();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new CoinGeckoService(new StubHttpClientFactory(client), configuration, cache);
        var cancellationToken = TestContext.Current.CancellationToken;

        var firstResult = await service.GetPricesAsync(["bitcoin", "ethereum"], cancellationToken);
        var secondResult = await service.GetPricesAsync(["ethereum", "bitcoin"], cancellationToken);

        Assert.Equal(2, firstResult.Count);
        Assert.Equal(67187.12m, firstResult["bitcoin"]);
        Assert.Equal(3500.45m, secondResult["ethereum"]);
        Assert.Equal(1, handler.RequestCount);
        Assert.Contains("bitcoin", handler.LastRequestUri, StringComparison.Ordinal);
        Assert.Contains("ethereum", handler.LastRequestUri, StringComparison.Ordinal);
        Assert.Equal("demo-key", handler.LastApiKey);
    }

    [Fact]
    public async Task GetTopMarketsAsync_ReturnsNormalizedMarketsAndUsesCache()
    {
        var handler = new StubHandler
        {
            ResponseContent = """
                [
                  {
                    "id": "bitcoin",
                    "symbol": "btc",
                    "name": "Bitcoin",
                    "current_price": 67187.12,
                    "market_cap_rank": 1,
                    "last_updated": "2026-07-26T04:30:00.000Z"
                  },
                  {
                    "id": "invalid",
                    "symbol": "bad",
                    "name": "Invalid",
                    "current_price": null,
                    "market_cap_rank": 2,
                    "last_updated": "2026-07-26T04:30:00.000Z"
                  }
                ]
                """
        };
        var client = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CoinGecko:ApiKey"] = "demo-key",
                ["CoinGecko:UniverseCacheMinutes"] = "60"
            })
            .Build();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new CoinGeckoService(new StubHttpClientFactory(client), configuration, cache);
        var cancellationToken = TestContext.Current.CancellationToken;

        var firstResult = await service.GetTopMarketsAsync(100, cancellationToken);
        var secondResult = await service.GetTopMarketsAsync(100, cancellationToken);

        var bitcoin = Assert.Single(firstResult);
        Assert.Equal("bitcoin", bitcoin.ExternalId);
        Assert.Equal("BTC", bitcoin.Symbol);
        Assert.Equal(67187.12m, bitcoin.Price);
        Assert.Equal(1, bitcoin.MarketCapRank);
        Assert.Equal(DateTimeKind.Utc, bitcoin.AsOf.Kind);
        Assert.Single(secondResult);
        Assert.Equal(1, handler.RequestCount);
        Assert.Contains("/coins/markets", handler.LastRequestUri, StringComparison.Ordinal);
        Assert.Contains("per_page=100", handler.LastRequestUri, StringComparison.Ordinal);
        Assert.Equal("demo-key", handler.LastApiKey);
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string LastRequestUri { get; private set; } = string.Empty;
        public string? LastApiKey { get; private set; }
        public string ResponseContent { get; init; } =
            """{"bitcoin":{"usd":67187.12},"ethereum":{"usd":3500.45}}""";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestUri = request.RequestUri?.ToString() ?? string.Empty;
            LastApiKey = request.Headers.TryGetValues("x-cg-demo-api-key", out var values)
                ? values.Single()
                : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseContent)
            });
        }
    }
}
