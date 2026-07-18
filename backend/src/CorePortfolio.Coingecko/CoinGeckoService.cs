using System.Text.Json;
using System.Text.RegularExpressions;
using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CorePortfolio.Coingecko;

public class CoinGeckoService : ICryptoPriceService, IPriceProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public CoinGeckoService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public string Source => "CoinGecko";

    public async Task<PriceQuote?> GetQuoteAsync(string symbolOrExternalId, string currency, CancellationToken cancellationToken = default)
    {
        var price = await GetPriceAsync(symbolOrExternalId, cancellationToken);
        return price is null ? null : new PriceQuote(price.Value, currency.ToUpperInvariant(), Source, DateTime.UtcNow, "Fresh");
    }

    public async Task<decimal?> GetPriceAsync(string coinId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(coinId) || !Regex.IsMatch(coinId, @"^[a-z0-9\-]+$"))
        {
            return null;
        }

        var client = _httpClientFactory.CreateClient("CoinGecko");
        var apiKey = _configuration["CoinGecko:ApiKey"];
        var url = $"https://api.coingecko.com/api/v3/simple/price?vs_currencies=usd&ids={coinId}";
        if (!string.IsNullOrWhiteSpace(apiKey))
            url += $"&x_cg_demo_api_key={Uri.EscapeDataString(apiKey)}";

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
