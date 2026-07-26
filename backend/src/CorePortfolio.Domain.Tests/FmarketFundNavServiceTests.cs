using System.Net;
using CorePortfolio.Fmarket;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CorePortfolio.Domain.Tests;

public sealed class FmarketFundNavServiceTests
{
    [Fact]
    public async Task GetFundsAsync_ParsesFundsAndCachesResponse()
    {
        const string json = """
        {"data":{"rows":[
          {"id":69,"shortName":"LPBF","name":"Quỹ Đầu tư Trái phiếu","nav":10727.29,
           "dataFundAssetType":{"name":"Trái phiếu"},"productNavChange":{"updateAt":1753056000000}},
          {"id":"70","shortName":"VCBF-FIF","name":"VCBF","nav":"16012.69"}
        ]}}
        """;
        var handler = new StubHandler(json);
        var service = new FmarketFundNavService(
            new StubClientFactory(new HttpClient(handler) { BaseAddress = new Uri("https://api.fmarket.vn/") }),
            new MemoryCache(new MemoryCacheOptions()),
            new ConfigurationBuilder().Build());

        var first = await service.GetFundsAsync(TestContext.Current.CancellationToken);
        var second = await service.GetFundsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, first.Count);
        Assert.Equal("69", first[0].ExternalId);
        Assert.Equal(10727.29m, first[0].Nav);
        Assert.Equal("VCBF-FIF", first[1].Symbol);
        Assert.Same(first, second);
        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class StubClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
