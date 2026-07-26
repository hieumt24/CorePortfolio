using CorePortfolio.API.Services;
using CorePortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Admin.Migration;

public sealed record RestoreDatabaseRequest(string FileName, string Confirmation);

public static class MigrationEndpoints
{
    public static void MapMigrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/migration")
            .WithTags("Admin - Migration")
            .RequireAuthorization("Admin");

        group.MapGet("/backups", async (
            BackupService backupService,
            CancellationToken cancellationToken) =>
            Results.Ok(await backupService.ListBackupsAsync(cancellationToken)))
            .RequireAuthorization("AdminRecovery");

        group.MapPost("/backup", async (
            BackupService backupService,
            AuditWriter auditWriter,
            AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var backup = await backupService.CreateBackupAsync(cancellationToken);
            auditWriter.Add("DatabaseBackupCreated", "DatabaseBackup", backup.FileName,
                new { backup.SizeBytes, backup.Sha256 });
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(backup);
        }).RequireAuthorization("AdminOperationsExecute");

        group.MapPost("/restore", async (
            [FromBody] RestoreDatabaseRequest request,
            BackupService backupService,
            AuditWriter auditWriter,
            AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var result = await backupService.RestoreAsync(
                request.FileName,
                request.Confirmation,
                cancellationToken);
            auditWriter.Add("DatabaseRestored", "DatabaseBackup", result.RestoredFileName,
                new { result.SafetyBackupFileName });
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("AdminRolesManage");

        group.MapPost("/run-legacy", async (
            MigrationService migrationService,
            AuditWriter auditWriter,
            AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            await migrationService.MigrateLegacyTransactionsAsync(cancellationToken);
            auditWriter.Add("LegacyMigrationRun", "Database", null);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { message = "Legacy data migration completed." });
        }).RequireAuthorization("AdminRolesManage");
    }
}
