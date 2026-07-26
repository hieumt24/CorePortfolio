using CorePortfolio.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CorePortfolio.API.IntegrationTests;

public sealed class SprintZeroMigrationTests
{
    private const string PreviousMigration = "20260719062712_AddAdminUserAccess";

    [Fact]
    public async Task Migration_RepairsLegacyDataBeforeAddingSnapshotConstraint()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, cancellationToken);

        var userId = Guid.NewGuid().ToString();
        var portfolioId = Guid.NewGuid().ToString();
        var cashAccountId = Guid.NewGuid().ToString().ToLowerInvariant();
        var ledgerEntryId = Guid.NewGuid().ToString().ToLowerInvariant();
        var snapshotDate = "2026-07-26 00:00:00";

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Users (Id, Username, PasswordHash, Role, IsActive, CreatedAt)
            VALUES ({userId}, 'migration-user', 'unused', 'User', 1, '2026-07-26 00:00:00');

            INSERT INTO Portfolios (Id, Name, Description, CreatedAt, UserId)
            VALUES ({portfolioId}, 'Migration portfolio', '', '2026-07-26 00:00:00', {userId});

            INSERT INTO CashAccounts (Id, PortfolioId, Currency)
            VALUES ({cashAccountId}, {portfolioId}, 'VND');

            INSERT INTO CashLedgerEntries
                (Id, CashAccountId, Amount, Type, Description, OccurredAt)
            VALUES
                ({ledgerEntryId}, {cashAccountId}, 1000, 0, 'Legacy entry', '2026-07-26 00:00:00');

            INSERT INTO PortfolioSnapshots
                (Id, PortfolioId, Date, TotalInvested, TotalValue, BaseCurrency,
                 UsdToVndRate, ValuationTimestamp, QualityStatus)
            VALUES
                ({Guid.NewGuid()}, {portfolioId}, {snapshotDate}, 900, 1000, 'VND',
                 26000, '2026-07-26 01:00:00', 'Complete'),
                ({Guid.NewGuid()}, {portfolioId}, {snapshotDate}, 900, 1100, 'VND',
                 26000, '2026-07-26 02:00:00', 'Complete');
            """, cancellationToken);

        await migrator.MigrateAsync(cancellationToken: cancellationToken);

        Assert.Equal(1, await db.PortfolioSnapshots.CountAsync(cancellationToken));
        var snapshot = await db.PortfolioSnapshots.SingleAsync(cancellationToken);
        Assert.Equal(1_100m, snapshot.HoldingsValue);
        Assert.Equal(1_000m, snapshot.CashValue);
        Assert.Equal(2_100m, snapshot.NetAssetValue);
        Assert.Equal(snapshot.NetAssetValue, snapshot.TotalValue);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT account.Id, entry.Id, entry.CashAccountId
            FROM CashAccounts account
            INNER JOIN CashLedgerEntries entry ON entry.CashAccountId = account.Id;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken));
        Assert.Equal(cashAccountId.ToUpperInvariant(), reader.GetString(0));
        Assert.Equal(ledgerEntryId.ToUpperInvariant(), reader.GetString(1));
        Assert.Equal(cashAccountId.ToUpperInvariant(), reader.GetString(2));
    }
}
