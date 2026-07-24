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
- Recurring cashflow foundation: `RecurringCashflowRule` entity, persistence schema, and `/api/recurring-cashflows` list/create/toggle endpoints. Scheduler, idempotent occurrence generation, and management page remain to be completed.
- Notification Center foundation: `Notification` entity, persistence schema, and `/api/notifications` list/read/read-all endpoints, plus Navbar unread popover. Alert evaluation rules and richer notification UX remain to be completed.
- Market price foundation: `PriceQuote`/`IPriceProvider` contracts, keyless-capable CoinGecko adapter, and `MarketPriceRefreshService` refreshing stale `CoinGecko` assets on a configurable 30-minute interval. CoinGecko IDs are batched into one `/simple/price` request per cycle and cached for 5 minutes to prevent duplicate background/admin calls; configure production with `MarketPrices__CryptoRefreshIntervalSeconds` (300-86400) and `CoinGecko__CacheSeconds` (20-3600). Legacy assets with an empty source are normalized by category: crypto → CoinGecko with known ID mappings, stocks/ETFs → DNSE, and open funds → Manual until Fund NAV is implemented. The resolver also repairs missing or outdated CoinGecko IDs for known symbols (including CMC20, HYPE, LINK, NEAR, NIGHT, and SOL). Transient upstream timeout/502/503/504 failures preserve the last known price and mark the asset `Stale`; permanent data errors remain `Error`. DNSE request timeout is configurable through `DNSE__TimeoutSeconds` (default 60 seconds, accepted range 10-120) to accommodate production network latency. DNSE session scheduling, price history, Fund NAV, and frontend status UI remain in the approved roadmap.

- Admin operations console: `/admin/overview` aggregates platform, user, and market-data health; `/admin/users` provides searchable, paginated access management. `GET /api/admin/overview`, `GET /api/admin/users`, and `PUT /api/admin/users/{id}/access` are protected by the `Admin` authorization policy. User access state is stored in `User.IsActive`, login activity in `User.LastLoginAt`, and the access handler prevents self-demotion, self-lockout, and removal of the last active administrator. JWT validation rechecks active state and role so lockouts and role changes take effect on the next request.
- Admin Market Assets: `/admin/market-assets` supports server-side search by symbol/name/external ID, category/source/status filters, sortable columns, and paginated results. The default order is symbol ascending (alphabetical), with deterministic ID tie-breaking.
- Telegram expense capture: `/chi [amount] "[category]" "[description]" [yyyy-MM-dd|dd/MM/yyyy]` records an expense for the earliest-created active Admin and that Admin's earliest-created portfolio. A successful command atomically creates the linked `CashflowRecord`, fiat withdrawal `Transaction`, and cash-ledger entry. `/cf` remains available for backward-compatible income/expense capture.
- Transaction tracking UX: `/transactions` now groups the global ledger into Crypto, Cổ phiếu, and CCQ/ETF tabs (with Vietnamese/English category matching), quick counts, category-aware add-transaction entry point, and an Edit action that reuses the existing user-scoped update endpoint. The add modal shows loading feedback while assets/categories are being resolved, a ledger-aligned payment/receipt total including fees, and a custom themed date-time picker. The existing user-scoped transaction API contract remains unchanged.
- Transaction file transfer: the `/transactions` ledger has a shared All/Crypto/Stock/Fund scope for import and export. It supports CSV, SpreadsheetML/binary XLS, and paginated PDF; PDF import understands official OKX and Binance Spot trading-history layouts. Binance fills preserve UTC+7 timestamps and normalize base-, quote-, or BNB-denominated fees into the transaction quote currency using the nearest BNB quote found in the report. Import opens a review step before any write, classifies ready/duplicate/invalid/out-of-scope rows, exposes missing Market Assets and portfolio assets, and lets Admins create and attach missing symbols inline. Duplicate fingerprints are skipped, assets and portfolios are resolved by ID/name, and writes continue through the existing validated transaction command.
- Transaction PDF export: generated reports use a landscape dashboard layout with scope/date metadata, KPI cards, readable transaction columns, type badges, page numbering, and embedded CorePortfolio data for lossless re-import.
- Transaction bulk deletion: authenticated users can delete all transactions or one asset group through `DELETE /api/transactions?assetGroup=All|Crypto|Stock|Fund`. The operation is user-scoped and atomic, removes linked cash-ledger entries and transaction-generated cashflows, and preserves portfolios, assets, and cash accounts.

- Crypto rewards are recorded with `TransactionType.Earn`: they increase holding quantity at zero acquisition value (apart from an optional capitalized fee), create no purchase cash flow, and are rejected for non-crypto categories. A crypto sale above the tracked quantity infers only the missing amount as an untracked zero-cost reward; Stock and CCQ/ETF retain strict oversell validation. Accounting uses weighted-average cost, applies acquisitions before sales at the same timestamp, and reports realized, unrealized, and combined PnL separately. Portfolio category headers show combined PnL percentages for Stock, CCQ/ETF, Crypto, and any configured category/currency group; performance analytics includes realized PnL and closed positions.

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
