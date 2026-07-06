using CorePortfolio.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CorePortfolio.API.Features.Admin.Migration;

public static class MigrationEndpoints
{
    public static void MapMigrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/migration")
            .WithTags("Admin - Migration")
            .RequireAuthorization("Admin");

        group.MapPost("/backup", (BackupService backupService) =>
        {
            var path = backupService.BackupDatabase();
            return Results.Ok(new { message = "Backup thành công", path });
        });

        group.MapPost("/run-legacy", async (MigrationService migrationService, CancellationToken cancellationToken) =>
        {
            await migrationService.MigrateLegacyTransactionsAsync(cancellationToken);
            return Results.Ok(new { message = "Migration dữ liệu cũ thành công" });
        });
    }
}
