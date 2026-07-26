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
              { "symbol": "HPG", "name": "CTCP Tập đoàn Hòa Phát", "nameEn": "Hoa Phat Group", "exchange": "HOSE", "type": "stock", "re": 22400 },
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
        Assert.Equal(22400m, hpg.ReferencePrice);
        Assert.Single(second);
        Assert.Equal("FUEVFVND", second[0].Symbol);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetGroupInstrumentsAsync_JoinsVn100SymbolsWithCachedCatalog()
    {
        var handler = new RouteStubHandler();
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

        var instruments = await service.GetGroupInstrumentsAsync(
            "VN100",
            TestContext.Current.CancellationToken);

        Assert.Equal(2, instruments.Count);
        Assert.Equal("HPG", instruments[0].Symbol);
        Assert.Equal("CTCP Tập đoàn Hòa Phát", instruments[0].Name);
        Assert.Equal(22400m, instruments[0].ReferencePrice);
        Assert.Equal("ACB", instruments[1].Symbol);
        Assert.Equal(2, handler.RequestCount);
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

    private sealed class RouteStubHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            var body = request.RequestUri?.AbsolutePath.EndsWith("/index/100/stocks", StringComparison.Ordinal) == true
                ? """{"status":200,"data":["HPG","ACB"]}"""
                : """
                  [
                    { "symbol": "HPG", "name": "CTCP Tập đoàn Hòa Phát", "exchange": "HOSE", "type": "stock", "re": 22400 },
                    { "symbol": "ACB", "name": "Ngân hàng TMCP Á Châu", "exchange": "HOSE", "type": "stock", "re": 25100 }
                  ]
                  """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            });
        }
    }
}
