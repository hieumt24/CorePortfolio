using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CorePortfolio.DNSE;

public class DnseStockInstrumentService : IStockInstrumentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DnseStockInstrumentService> _logger;

    public DnseStockInstrumentService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<DnseStockInstrumentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IEnumerable<StockInstrument>> SearchInstrumentsAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (!DnseConfiguration.TryGetCredentials(_configuration, out var apiKey, out var secretKey, out var missingSetting))
        {
            _logger.LogWarning("DNSE instruments request skipped because the following configuration is missing: {MissingSetting}.", missingSetting);
            return Enumerable.Empty<StockInstrument>();
        }

        string method = "GET";
        string path = $"/instruments?limit={limit}";
        if (!string.IsNullOrWhiteSpace(query))
        {
            path += $"&symbol={query.Trim().ToUpper()}";
        }
        
        var now = DateTime.UtcNow;
        string dateStr = now.ToString("ddd, dd MMM yyyy HH:mm:ss +0000", CultureInfo.InvariantCulture);
        string nonce = Guid.NewGuid().ToString("N").ToLower();

        // Note: For /instruments, DNSE expects the base path in request-target, and x-aux-date instead of Date
        string signingString = $"(request-target): {method.ToLower()} /instruments\nx-aux-date: {dateStr}\nnonce: {nonce}";
        string encodedSignature = GenerateSignature(secretKey.Trim(), signingString);

        string xSignature = $"Signature keyId=\"{apiKey.Trim()}\",algorithm=\"hmac-sha256\",headers=\"(request-target) x-aux-date\",signature=\"{encodedSignature}\",nonce=\"{nonce}\"";

        var client = _httpClientFactory.CreateClient(DnseConfiguration.HttpClientName);
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        request.Headers.Clear();
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-API-Key", apiKey.Trim());
        request.Headers.TryAddWithoutValidation("X-Signature", xSignature);
        request.Headers.TryAddWithoutValidation("X-Aux-Date", dateStr);
        request.Headers.TryAddWithoutValidation("version", DnseConfiguration.GetApiVersion(_configuration));

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("DNSE Instruments API returned {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);
                return new List<StockInstrument> 
                { 
                    new StockInstrument { Symbol = "ERROR", Name = $"HTTP {response.StatusCode}: {responseBody}" } 
                };
            }

            using var document = JsonDocument.Parse(responseBody);
            var instruments = new List<StockInstrument>();

            if (document.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataElement.EnumerateArray())
                {
                    var instrument = new StockInstrument
                    {
                        Symbol = item.TryGetProperty("symbol", out var sym) ? sym.GetString() ?? "" : "",
                        MarketId = item.TryGetProperty("marketId", out var mk) ? mk.GetString() ?? "" : "",
                        SecurityGroupId = item.TryGetProperty("securityGroupId", out var sg) ? sg.GetString() ?? "" : "",
                        ShortName = item.TryGetProperty("shortName", out var sn) ? sn.GetString() ?? "" : "",
                        Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : ""
                    };

                    if (item.TryGetProperty("indexName", out var idxElement) && idxElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var idx in idxElement.EnumerateArray())
                        {
                            var idxStr = idx.GetString();
                            if (!string.IsNullOrEmpty(idxStr))
                            {
                                instrument.IndexName.Add(idxStr);
                            }
                        }
                    }

                    instruments.Add(instrument);
                }
            }

            return instruments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while searching instruments from DNSE.");
            return new List<StockInstrument> 
            { 
                new StockInstrument { Symbol = "EXCEPTION", Name = ex.Message } 
            };
        }
    }

    private static string GenerateSignature(string secret, string signingString)
    {
        byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
        byte[] stringBytes = Encoding.UTF8.GetBytes(signingString);

        using (var hmac = new HMACSHA256(secretBytes))
        {
            byte[] hashBytes = hmac.ComputeHash(stringBytes);
            return Convert.ToBase64String(hashBytes)
                .Replace("+", "%2B")
                .Replace("/", "%2F")
                .Replace("=", "%3D");
        }
    }
}
