# CorePortfolio Project Context

## Purpose

CorePortfolio is a personal portfolio and cashflow application. It combines investment holdings, transactions, cash accounts, budgets, saving goals, DCA plans, rebalancing, analytics, and reports behind authenticated user-scoped APIs.

## Repository map

- `backend/src/CorePortfolio.Domain`: entities, enums, and accounting rules.
- `backend/src/CorePortfolio.Infrastructure`: EF Core `AppDbContext`, SQLite migrations, persistence services.
- `backend/src/CorePortfolio.API`: ASP.NET Core Minimal APIs organized by vertical feature slices; MediatR handlers live beside endpoints.
- `frontend/src`: React 19 + TypeScript + Vite application. Feature folders contain API clients, types, and components.
- `.agents/skills`: project-specific agent workflows.

## Backend conventions

- Entry point: `backend/src/CorePortfolio.API/Program.cs`.
- Map feature endpoints from `Program.cs`; use `RequireAuthorization()` for user data.
- Resolve identity through `ICurrentUserService.UserId` and filter every user-owned query.
- Use UTC dates and explicit VND/USD conversion through `ExchangeRateService`.
- CORS is configured by `Cors:AllowedOrigins`; production also permits HTTPS Vercel preview origins. Redeploy the API after changing this policy.
- Health checks: `/health/live` only verifies the process, while `/health/ready` (also exposed through `/health`) verifies database connectivity. Azure Linux should persist SQLite at `/home/data/CorePortfolio.db` or set `ConnectionStrings__DefaultConnection` explicitly.
- Database migration failures are logged as critical startup diagnostics but do not terminate the process; liveness remains reachable and readiness reports the database failure.
- Add entities to `AppDbContext`, configure relationships/indexes in `OnModelCreating`, and create an EF migration for schema changes.
- Preserve the vertical-slice + MediatR pattern; do not introduce MVC controllers.

## Frontend conventions

- Routes are registered in `frontend/src/app/App.tsx`.
- Shared navigation is `frontend/src/shared/components/Navbar.tsx`.
- HTTP calls use `frontend/src/shared/api/baseClient.ts`.
- Feature API clients and TypeScript contracts live under `frontend/src/features/*/api` and `types`.
- Visual language uses the premium glassmorphism tokens in `frontend/src/app/index.css`; preserve responsive states and explicit loading/error/empty UI.

## Current feature surface

- Portfolio, asset, transaction, cashflow, cash account, analytics, report, watchlist, budget, saving goal, rebalancing, and DCA flows are present.
- Financial Health Center aggregate: `GET /api/dashboard/financial-health` and the dashboard integration.
- Recurring cashflow foundation: `RecurringCashflowRule` entity and `/api/recurring-cashflows` list/create/toggle endpoints. Scheduler, idempotent occurrence generation, migration, and management page remain to be completed.
- Notification Center foundation: `Notification` entity and `/api/notifications` list/read/read-all endpoints, plus Navbar unread popover. Alert evaluation rules, persistence migration, and richer notification UX remain to be completed.
- Market price foundation: `PriceQuote`/`IPriceProvider` contracts, keyless-capable CoinGecko adapter, and `MarketPriceRefreshService` refreshing `CoinGecko` assets on a configurable 60-second interval. DNSE session scheduling, price history, Fund NAV, and frontend status UI remain in the approved roadmap.

## Verification commands

```powershell
dotnet build backend/src/CorePortfolio.API/CorePortfolio.API.csproj
dotnet test backend/src/CorePortfolio.Domain.Tests/CorePortfolio.Domain.Tests.csproj
cd frontend
npm run build
```

## CI/CD

- `.github/workflows/backend-ci.yml` restores and builds the API project directly, runs domain tests, and verifies the EF snapshot.
- `.github/workflows/frontend-ci.yml` runs blocking `npm ci`, Vitest, and the production build; ESLint runs as an advisory step while legacy lint violations are migrated.
- `.github/workflows/main_coreportfolio-api.yml` restores the API with the `linux-x64` runtime target, builds/tests it, publishes a self-contained Linux artifact, transfers it as a tar archive to preserve executable permissions, checks `/health/live` as a blocking smoke test, and logs `/health/ready` database status.
- Production deployment requires the Azure publish-profile secret and the API App Service CORS configuration described above. Vercel frontend deployment remains managed by Vercel; frontend CI is the merge gate.

If an environment prevents backend restore/build, report the exact limitation; do not claim the backend is verified.

## Update policy

When a task changes routes, endpoints, entities, migrations, or feature status, update this document before final handoff. Keep entries concise and link to source paths when adding detailed contracts.
