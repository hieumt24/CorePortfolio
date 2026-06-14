using System.Text.Json;
using System.Text.RegularExpressions;
using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CorePortfolio.Coingecko;

public class CoinGeckoService : ICryptoPriceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public CoinGeckoService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<decimal?> GetPriceAsync(string coinId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(coinId) || !Regex.IsMatch(coinId, @"^[a-z0-9\-]+$"))
        {
            return null;
        }

        var apiKey = _configuration["CoinGecko:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        var client = _httpClientFactory.CreateClient("CoinGecko");
        var url = $"https://api.coingecko.com/api/v3/simple/price?vs_currencies=usd&ids={coinId}&x_cg_demo_api_key={apiKey}";

        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var document = await JsonDocument.ParseAsync(contentStream, default, cancellationToken);

            if (document.RootElement.TryGetProperty(coinId, out var coinElement))
            {
                if (coinElement.TryGetProperty("usd", out var usdElement))
                {
                    if (usdElement.TryGetDecimal(out var price))
                    {
                        return price;
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
