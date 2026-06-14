using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CorePortfolio.DNSE;

public class DnseStockPriceService : IStockPriceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DnseStockPriceService> _logger;

    public DnseStockPriceService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<DnseStockPriceService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<decimal?> GetStockPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return null;

        var apiKey = _configuration["DNSE:ApiKey"];
        var secretKey = _configuration["DNSE:SecretKey"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(secretKey))
        {
            _logger.LogWarning("DNSE API Key or Secret Key is not configured.");
            return null;
        }

        string method = "GET";
        string path = $"/price/{symbol.ToUpper()}/secdef";
        
        var now = DateTime.UtcNow;
        string dateStr = now.ToString("ddd, dd MMM yyyy HH:mm:ss +0000", CultureInfo.InvariantCulture);
        string nonce = Guid.NewGuid().ToString("N").ToLower();

        string signingString = $"(request-target): {method.ToLower()} {path}\ndate: {dateStr}\nnonce: {nonce}";
        string encodedSignature = GenerateSignature(secretKey.Trim(), signingString);

        string xSignature = $"Signature keyId=\"{apiKey.Trim()}\",algorithm=\"hmac-sha256\",headers=\"(request-target) date\",signature=\"{encodedSignature}\",nonce=\"{nonce}\"";

        var client = _httpClientFactory.CreateClient("DNSE");
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://openapi.dnse.com.vn{path}");

        request.Headers.Clear();
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("x-api-key", apiKey.Trim());
        request.Headers.TryAddWithoutValidation("x-Signature", xSignature);
        request.Headers.TryAddWithoutValidation("Date", dateStr);
        request.Headers.TryAddWithoutValidation("version", "2026-05-07");

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("DNSE API returned {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);
                return null;
            }

            using var document = JsonDocument.Parse(responseBody);
            
            // Expected response is a JSON array
            if (document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0)
            {
                var firstElement = document.RootElement[0];
                if (firstElement.TryGetProperty("basicPrice", out var basicPriceElement))
                {
                    if (basicPriceElement.TryGetDecimal(out var price))
                    {
                        // DNSE returns price in thousands (e.g. 73.5), convert to absolute VND (73500)
                        return price * 1000m;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting stock price for {Symbol} from DNSE.", symbol);
            return null;
        }
    }

    private static string GenerateSignature(string secret, string signingString)
    {
        byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
        byte[] stringBytes = Encoding.UTF8.GetBytes(signingString);

        using (var hmac = new HMACSHA256(secretBytes))
        {
            byte[] hashBytes = hmac.ComputeHash(stringBytes);
            string base64String = Convert.ToBase64String(hashBytes);

            string encodedSignature = base64String
                .Replace("+", "%2B")
                .Replace("/", "%2F")
                .Replace("=", "%3D");

            return encodedSignature;
        }
    }
}
