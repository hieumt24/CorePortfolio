using Microsoft.Extensions.Configuration;
using System.IO;

namespace CorePortfolio.API.Services;

public class BackupService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IConfiguration configuration, ILogger<BackupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string BackupDatabase()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        string dbPath = "CorePortfolio.db";
        
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    dbPath = part.Substring("Data Source=".Length).Trim();
                    break;
                }
            }
        }

        if (!File.Exists(dbPath))
        {
            throw new FileNotFoundException($"Không tìm thấy file database tại {dbPath}");
        }

        var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        if (!Directory.Exists(backupDir))
        {
            Directory.CreateDirectory(backupDir);
        }

        var backupPath = Path.Combine(backupDir, $"{Path.GetFileNameWithoutExtension(dbPath)}_{DateTime.UtcNow:yyyyMMddHHmmss}.bak");
        File.Copy(dbPath, backupPath);
        _logger.LogInformation("Đã backup database ra file {BackupPath}", backupPath);
        return backupPath;
    }
}
