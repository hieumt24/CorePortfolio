using System.Net;
using CorePortfolio.KBS;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class KbsMarketIndexServiceTests
{
    [Fact]
    public async Task GetQuotesAsync_ReturnsLatestValuesAndChanges()
    {
        var handler = new StubHandler();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://kbbuddywts.kbsec.com.vn/iis-server/investment/")
        };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KbsMarketIndexService(
            new StubHttpClientFactory(client),
            new ConfigurationBuilder().Build(),
            cache,
            NullLogger<KbsMarketIndexService>.Instance);

        var quotes = await service.GetQuotesAsync(
            ["VNINDEX", "VN30"],
            TestContext.Current.CancellationToken);

        Assert.Collection(
            quotes,
            quote =>
            {
                Assert.Equal("VNINDEX", quote.Symbol);
                Assert.Equal(1280.5m, quote.Value);
                Assert.Equal(10.5m, quote.Change);
                Assert.Equal(10.5m / 1270m * 100m, quote.ChangePercent);
                Assert.Equal("Fresh", quote.Status);
            },
            quote =>
            {
                Assert.Equal("VN30", quote.Symbol);
                Assert.Equal(1405.25m, quote.Value);
                Assert.Equal(-4.75m, quote.Change);
                Assert.Equal("Fresh", quote.Status);
            });
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task GetQuotesAsync_IsolatesUpstreamFailurePerIndex()
    {
        var handler = new StubHandler(failVn30: true);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://kbbuddywts.kbsec.com.vn/iis-server/investment/")
        };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KbsMarketIndexService(
            new StubHttpClientFactory(client),
            new ConfigurationBuilder().Build(),
            cache,
            NullLogger<KbsMarketIndexService>.Instance);

        var quotes = await service.GetQuotesAsync(
            ["VNINDEX", "VN30"],
            TestContext.Current.CancellationToken);

        Assert.Equal("Fresh", quotes[0].Status);
        Assert.Equal("Error", quotes[1].Status);
        Assert.NotNull(quotes[1].Error);
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(bool failVn30 = false) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            var isVn30 = request.RequestUri?.AbsolutePath.Contains(
                "/index/VN30/",
                StringComparison.Ordinal) == true;
            if (failVn30 && isVn30)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway));

            var response = isVn30
                ? """{"data_day":[{"t":"2026-07-24 15:00","c":1410.00},{"t":"2026-07-25 15:00","c":1405.25}]}"""
                : """{"data_day":[{"t":"2026-07-25 15:00","c":1280.50},{"t":"2026-07-24 15:00","c":1270.00}]}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            });
        }
    }
}
