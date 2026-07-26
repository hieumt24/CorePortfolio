# Production hardening runbook

## Persistent storage

The Azure Linux App Service must keep both the live SQLite file and its backups on persistent storage:

```text
ConnectionStrings__DefaultConnection=Data Source=/home/data/CorePortfolio.db
Backups__Directory=/home/data/backups
Backups__RetentionCount=10
Backups__ListLimit=30
```

Do not place the database or backup directory under the deployed application package. The package is replaced on deployment.

## Online backup

Admin endpoints:

- `GET /api/admin/migration/backups` lists retained backups without exposing server paths.
- `POST /api/admin/migration/backup` uses SQLite online backup, calculates SHA-256, runs `PRAGMA quick_check`, and only returns success when the copied database is valid.

Create a backup before every manual data repair or migration. Copy critical backups from `/home/data/backups` to independent storage on a schedule; retention on the App Service is not a disaster-recovery substitute.

## Validated restore

Restore is intentionally a two-part request:

```json
{
  "fileName": "CorePortfolio_20260726T080000000Z_manual.db",
  "confirmation": "RESTORE"
}
```

Send it to `POST /api/admin/migration/restore` as an authenticated Admin. The API rejects path traversal and non-managed file names, requires the backup migration version to match the running schema, validates the source, enters maintenance mode, creates a pre-restore safety backup, restores through SQLite's online backup API, and validates the resulting live database. If validation fails, it restores the safety copy.

During restore, mutating requests return HTTP 503 and `/health/ready` returns HTTP 503. Liveness remains available. After a successful restore:

1. Confirm `/health/ready` returns 200.
2. Check `/api/admin/operations`.
3. Verify recent user, portfolio, and transaction counts.
4. Retain the pre-restore safety backup until business validation is complete.

## Audit and job operations

- `GET /api/admin/audit-events` supports `action`, `actorUserId`, `from`, `to`, `page`, and `pageSize`.
- Audit records include actor, action, target, outcome, request IP, correlation ID, metadata, and UTC timestamp.
- Current audited actions include user access changes, benchmark configuration/manual prices, database backup/restore, and legacy migration runs.
- `GET /api/admin/operations` exposes maintenance state plus the last start/success/failure, duration, counters, and error for daily snapshot and market-price refresh jobs.

Audit records intentionally do not use a foreign key to `Users`, so an account deletion cannot erase the historical actor identifier.

## Optimistic concurrency

`MarketAsset`, `Budget`, `SavingGoal`, `DcaPlan`, `RebalanceExecutionPlan`, and `NotificationPreference` carry an EF concurrency `Version`. Every tracked update advances the version. Overlapping writes based on an older version produce HTTP 409 through the global problem-details handler.

Clients should reload the resource after HTTP 409 and ask the user to reapply their change instead of silently retrying a stale payload.

## Release gates and rollback

Backend CI and the production workflow apply every EF migration to a new SQLite database before publish. Production deployment is successful only when both `/health/live` and `/health/ready` return 200.

Recommended rollback sequence:

1. Stop new writes or enable the App Service maintenance page.
2. Preserve a current online backup.
3. Redeploy the previous known-good artifact.
4. If the schema/data itself must be rolled back, restore a validated pre-deployment backup through the Admin restore endpoint.
5. Recheck readiness and core business counts before reopening writes.

Never downgrade SQLite by manually deleting migration-history rows.
