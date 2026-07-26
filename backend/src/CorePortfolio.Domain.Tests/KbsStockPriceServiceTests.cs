using System.Net;
using CorePortfolio.KBS;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class KbsStockPriceServiceTests
{
    [Fact]
    public async Task GetStockPriceAsync_ReturnsLatestAbsoluteVndCloseAndUsesCache()
    {
        var handler = new StubHandler(
            """
            {
              "data_day": [
                { "t": "2026-07-24 07:00", "c": 22400 },
                { "t": "2026-07-25 07:00", "c": 22650 },
                { "t": "2026-07-23 07:00", "c": 22100 }
              ]
            }
            """);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://kbbuddywts.kbsec.com.vn/iis-server/investment/")
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KBS:LookbackDays"] = "14",
                ["KBS:PriceCacheSeconds"] = "300"
            })
            .Build();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KbsStockPriceService(
            new StubHttpClientFactory(client),
            configuration,
            cache,
            NullLogger<KbsStockPriceService>.Instance);
        var cancellationToken = TestContext.Current.CancellationToken;

        var first = await service.GetStockPriceAsync("hpg", cancellationToken);
        var second = await service.GetStockPriceAsync("HPG", cancellationToken);

        Assert.Equal(22650m, first);
        Assert.Equal(first, second);
        Assert.Equal(1, handler.RequestCount);
        Assert.Contains("stocks/HPG/data_day", handler.LastRequestUri, StringComparison.Ordinal);
        Assert.Contains("sdate=", handler.LastRequestUri, StringComparison.Ordinal);
        Assert.Contains("edate=", handler.LastRequestUri, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("HPG!")]
    [InlineData("../HPG")]
    public async Task GetStockPriceAsync_RejectsInvalidSymbol(string symbol)
    {
        var client = new HttpClient(new StubHandler("""{"data_day":[]}"""))
        {
            BaseAddress = new Uri("https://kbbuddywts.kbsec.com.vn/iis-server/investment/")
        };
        var configuration = new ConfigurationBuilder().Build();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KbsStockPriceService(
            new StubHttpClientFactory(client),
            configuration,
            cache,
            NullLogger<KbsStockPriceService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetStockPriceAsync(symbol, TestContext.Current.CancellationToken));
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(string responseBody) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string LastRequestUri { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestUri = request.RequestUri?.ToString() ?? string.Empty;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
