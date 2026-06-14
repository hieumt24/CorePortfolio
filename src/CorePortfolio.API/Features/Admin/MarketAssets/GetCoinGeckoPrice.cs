using MediatR;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CorePortfolio.API.Features.Admin.MarketAssets;

public record GetCoinGeckoPriceQuery(string CoinId) : IRequest<decimal?>;

public class GetCoinGeckoPriceHandler : IRequestHandler<GetCoinGeckoPriceQuery, decimal?>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public GetCoinGeckoPriceHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<decimal?> Handle(GetCoinGeckoPriceQuery request, CancellationToken cancellationToken)
    {
        // Security Check: Validate CoinId to prevent SSRF or Injection
        if (string.IsNullOrWhiteSpace(request.CoinId) || !Regex.IsMatch(request.CoinId, @"^[a-z0-9\-]+$"))
        {
            return null;
        }

        var apiKey = _configuration["CoinGecko:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            // Fallback or log error
            return null;
        }

        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.coingecko.com/api/v3/simple/price?vs_currencies=usd&ids={request.CoinId}&x_cg_demo_api_key={apiKey}";

        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var document = await JsonDocument.ParseAsync(contentStream, default, cancellationToken);

            if (document.RootElement.TryGetProperty(request.CoinId, out var coinElement))
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
            // Log error
            return null;
        }
    }
}
