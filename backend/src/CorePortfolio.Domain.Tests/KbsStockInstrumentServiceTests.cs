using System.Net;
using CorePortfolio.KBS;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class KbsStockInstrumentServiceTests
{
    [Fact]
    public async Task SearchInstrumentsAsync_FiltersAndCachesCatalog()
    {
        var handler = new StubHandler(
            """
            [
              { "symbol": "HPG", "name": "CTCP Tập đoàn Hòa Phát", "nameEn": "Hoa Phat Group", "exchange": "HOSE", "type": "stock" },
              { "symbol": "FUEVFVND", "name": "Quỹ ETF DCVFMVN DIAMOND", "exchange": "HOSE", "type": "etf" },
              { "symbol": "VNINDEX", "name": "VN Index", "exchange": "INDEX", "type": "index" }
            ]
            """);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://kbbuddywts.kbsec.com.vn/iis-server/investment/")
        };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KbsStockInstrumentService(
            new StubHttpClientFactory(client),
            new ConfigurationBuilder().Build(),
            cache,
            NullLogger<KbsStockInstrumentService>.Instance);
        var cancellationToken = TestContext.Current.CancellationToken;

        var first = (await service.SearchInstrumentsAsync("HPG", cancellationToken: cancellationToken)).ToArray();
        var second = (await service.SearchInstrumentsAsync("quỹ", cancellationToken: cancellationToken)).ToArray();

        var hpg = Assert.Single(first);
        Assert.Equal("HOSE", hpg.MarketId);
        Assert.Equal("CTCP Tập đoàn Hòa Phát", hpg.Name);
        Assert.Single(second);
        Assert.Equal("FUEVFVND", second[0].Symbol);
        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(string responseBody) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
