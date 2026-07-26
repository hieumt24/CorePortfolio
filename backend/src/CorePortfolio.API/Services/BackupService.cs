using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace CorePortfolio.API.Services;

public sealed record DatabaseBackupDescriptor(
    string FileName,
    long SizeBytes,
    string Sha256,
    DateTime CreatedAt,
    string SchemaVersion,
    string IntegrityStatus);

public sealed record DatabaseRestoreResult(
    string RestoredFileName,
    string SafetyBackupFileName,
    DateTime RestoredAt);

public sealed class BackupService
{
    private static readonly SemaphoreSlim OperationLock = new(1, 1);
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;
    private readonly ProductionOperationsState _operationsState;

    public BackupService(
        IConfiguration configuration,
        ILogger<BackupService> logger,
        ProductionOperationsState operationsState)
    {
        _configuration = configuration;
        _logger = logger;
        _operationsState = operationsState;
    }

    public async Task<IReadOnlyList<DatabaseBackupDescriptor>> ListBackupsAsync(
        CancellationToken cancellationToken)
    {
        var directory = GetBackupDirectory();
        if (!Directory.Exists(directory))
            return [];

        var files = Directory.EnumerateFiles(directory, "CorePortfolio_*.db")
            .OrderByDescending(File.GetCreationTimeUtc)
            .Take(Math.Clamp(_configuration.GetValue("Backups:ListLimit", 30), 1, 100))
            .ToArray();
        var result = new List<DatabaseBackupDescriptor>(files.Length);
        foreach (var file in files)
        {
            try
            {
                result.Add(await DescribeAsync(file, validateIntegrity: false, cancellationToken));
            }
            catch (Exception exception) when (exception is SqliteException or InvalidDataException)
            {
                _logger.LogWarning(exception, "Ignoring unreadable backup file {BackupFile}", Path.GetFileName(file));
            }
        }
        return result;
    }

    public async Task<DatabaseBackupDescriptor> CreateBackupAsync(
        CancellationToken cancellationToken)
    {
        await OperationLock.WaitAsync(cancellationToken);
        try
        {
            return await CreateBackupCoreAsync("manual", cancellationToken);
        }
        finally
        {
            OperationLock.Release();
        }
    }

    public async Task<DatabaseRestoreResult> RestoreAsync(
        string fileName,
        string confirmation,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(confirmation, "RESTORE", StringComparison.Ordinal))
            throw new ArgumentException("Restore confirmation must be exactly RESTORE.");

        var safeName = Path.GetFileName(fileName);
        if (!string.Equals(fileName, safeName, StringComparison.Ordinal) ||
            !safeName.StartsWith("CorePortfolio_", StringComparison.Ordinal) ||
            !safeName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid backup file name.");

        var backupPath = Path.Combine(GetBackupDirectory(), safeName);
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup file was not found.", safeName);

        await OperationLock.WaitAsync(cancellationToken);
        _operationsState.EnterMaintenance($"Restoring database from {safeName}");
        try
        {
            var sourceDescriptor = await DescribeAsync(backupPath, validateIntegrity: true, cancellationToken);
            if (!string.Equals(sourceDescriptor.IntegrityStatus, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Backup integrity check failed.");
            var liveSchemaVersion = await GetLatestMigrationAsync(
                GetConnectionString(),
                cancellationToken);
            if (!string.Equals(
                    sourceDescriptor.SchemaVersion,
                    liveSchemaVersion,
                    StringComparison.Ordinal))
                throw new InvalidDataException(
                    $"Backup schema {sourceDescriptor.SchemaVersion} is not compatible with live schema {liveSchemaVersion}.");

            var safetyBackup = await CreateBackupCoreAsync("pre-restore", cancellationToken);
            try
            {
                await RestoreCoreAsync(backupPath, cancellationToken);
                await ValidateDatabaseAsync(GetConnectionString(), cancellationToken);
            }
            catch
            {
                var safetyPath = Path.Combine(GetBackupDirectory(), safetyBackup.FileName);
                await RestoreCoreAsync(safetyPath, cancellationToken);
                throw;
            }

            _logger.LogWarning(
                "Database restore completed from {BackupFile}; safety backup {SafetyBackupFile}",
                safeName,
                safetyBackup.FileName);
            return new DatabaseRestoreResult(safeName, safetyBackup.FileName, DateTime.UtcNow);
        }
        finally
        {
            _operationsState.ExitMaintenance();
            OperationLock.Release();
        }
    }

    private async Task<DatabaseBackupDescriptor> CreateBackupCoreAsync(
        string reason,
        CancellationToken cancellationToken)
    {
        var directory = GetBackupDirectory();
        Directory.CreateDirectory(directory);
        var fileName = $"CorePortfolio_{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}_{reason}.db";
        var destinationPath = Path.Combine(directory, fileName);
        var destinationConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = destinationPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        await using (var source = new SqliteConnection(GetConnectionString()))
        await using (var destination = new SqliteConnection(destinationConnectionString))
        {
            await source.OpenAsync(cancellationToken);
            await destination.OpenAsync(cancellationToken);
            source.BackupDatabase(destination);
        }

        var descriptor = await DescribeAsync(destinationPath, validateIntegrity: true, cancellationToken);
        if (!string.Equals(descriptor.IntegrityStatus, "ok", StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(destinationPath);
            throw new InvalidDataException("The generated database backup failed its integrity check.");
        }

        PruneOldBackups(directory);
        _logger.LogInformation(
            "Online database backup created: {BackupFile}, {SizeBytes} bytes, sha256 {Sha256}",
            descriptor.FileName,
            descriptor.SizeBytes,
            descriptor.Sha256);
        return descriptor;
    }

    private async Task RestoreCoreAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var sourceConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();
        await using var source = new SqliteConnection(sourceConnectionString);
        await using var destination = new SqliteConnection(GetConnectionString());
        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);
        source.BackupDatabase(destination);
    }

    private async Task<DatabaseBackupDescriptor> DescribeAsync(
        string path,
        bool validateIntegrity,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(path);
        await using var stream = File.OpenRead(path);
        var checksum = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken))
            .ToLowerInvariant();
        var integrity = validateIntegrity
            ? await ValidateDatabaseAsync(
                new SqliteConnectionStringBuilder
                {
                    DataSource = path,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString(),
                cancellationToken)
            : "not-checked";
        var schemaVersion = await GetLatestMigrationAsync(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString(),
            cancellationToken);

        return new DatabaseBackupDescriptor(
            fileInfo.Name,
            fileInfo.Length,
            checksum,
            fileInfo.CreationTimeUtc,
            schemaVersion,
            integrity);
    }

    private static async Task<string> ValidateDatabaseAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var integrityCommand = connection.CreateCommand();
        integrityCommand.CommandText = "PRAGMA quick_check;";
        var integrity = Convert.ToString(
            await integrityCommand.ExecuteScalarAsync(cancellationToken)) ?? "unknown";
        if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
            return integrity;

        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory';";
        var hasMigrationHistory = Convert.ToInt32(
            await schemaCommand.ExecuteScalarAsync(cancellationToken)) == 1;
        if (!hasMigrationHistory)
            throw new InvalidDataException("Database does not contain EF migration history.");
        return integrity;
    }

    private static async Task<string> GetLatestMigrationAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;";
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken))
            ?? throw new InvalidDataException("Database does not contain an applied migration.");
    }

    private string GetConnectionString()
    {
        var configured = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;
        var path = OperatingSystem.IsLinux() ? "/home/data/CorePortfolio.db" : "CorePortfolio.db";
        return $"Data Source={path}";
    }

    private string GetBackupDirectory()
    {
        var configured = _configuration["Backups:Directory"];
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);
        return OperatingSystem.IsLinux()
            ? "/home/data/backups"
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Backups"));
    }

    private void PruneOldBackups(string directory)
    {
        var retention = Math.Clamp(_configuration.GetValue("Backups:RetentionCount", 10), 2, 100);
        foreach (var file in Directory.EnumerateFiles(directory, "CorePortfolio_*.db")
                     .OrderByDescending(File.GetCreationTimeUtc)
                     .Skip(retention))
        {
            File.Delete(file);
        }
    }
}
