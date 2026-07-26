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
- Sprint 0 data-integrity foundation: portfolio snapshots enforce one row per portfolio/day. Migration `AddSprintZeroDataIntegrity` removes legacy duplicate snapshots, normalizes legacy cash-account/ledger GUID casing once, and replaces the previous startup-time repair. API integration tests boot the real Minimal API against isolated in-memory SQLite databases and cover user isolation, transaction/ledger atomicity, snapshot uniqueness, and upgrading legacy data through the Sprint 0 migration.

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
- Notification Core: `Notification` now carries typed category/severity, entity/action metadata, expiry, read, and dismissal state. `NotificationPreference` stores user-scoped enablement and optional warning/critical thresholds; Budget defaults to 80%/100%. The MediatR notification slices expose paginated/filterable `GET /api/notifications`, `GET /api/notifications/unread-count`, read/read-all/dismiss commands, and preference GET/PUT endpoints. `NotificationWriter` is the idempotent write boundary: it honors preferences, requires a scoped dedupe key, handles uniqueness races, and returns Created/Duplicate/Suppressed outcomes. The Navbar uses the count endpoint and a five-item unread preview. Migration `AddNotificationCore` adds the schema; alert evaluators and the full Notification Center page remain for Sprint 2.
- User profile and security: `User` stores optional display name and email with a unique email index. Authenticated users can read/update only their own profile through `GET/PUT /api/profile` and rotate their password through `PUT /api/profile/password`, which verifies the current BCrypt hash. Registration now persists the existing frontend email field and requires an 8-character password. The `/profile` route provides responsive profile/password forms, while the N1b top Navbar places notification and conventional avatar menus on the right. Migration `AddUserProfile` adds the schema.
- Market price foundation: `PriceQuote`/`IPriceProvider` contracts, keyless CoinGecko crypto pricing, and a keyless KBS adapter for Vietnamese stocks/ETFs. `CorePortfolio.KBS` reads the latest daily close as absolute VND, caches prices for 5 minutes, caches the public instrument catalog for 6 hours, validates symbols, and needs no Python runtime or broker credentials. Background refresh handles stale CoinGecko and KBS assets on independent configurable intervals; use `MarketPrices__CryptoRefreshIntervalSeconds`, `MarketPrices__StockRefreshIntervalSeconds`, `CoinGecko__CacheSeconds`, and the `KBS__*` settings. Empty sources are normalized by category: crypto → CoinGecko, stocks/ETFs → KBS, and open funds → Manual; persisted legacy DNSE sources are migrated in place to KBS during refresh. Admin price lookup and autocomplete use `/api/admin/market-assets/kbs-price/{symbol}` and `/api/admin/market-assets/kbs-instruments`. Transient upstream failures preserve the last known price and mark it `Stale`. The KBS integration was researched from the locally cloned vnstock provider behavior but is implemented as an independent .NET HTTP adapter; its data usage is intended for this personal project and upstream terms must be re-evaluated before commercial use. A Vnstock account key activates quotas inside the Python `vnstock`/`vnai` runtime and must not be forwarded to KBS or stored in this .NET application; see `docs/PRODUCTION_MARKET_DATA.md` for the Azure runbook and an optional future Python-worker design. Price history and Fund NAV remain roadmap items.

- Admin operations console: `/admin/overview` aggregates platform, user, and market-data health; `/admin/users` provides searchable, paginated access management. `GET /api/admin/overview`, `GET /api/admin/users`, and `PUT /api/admin/users/{id}/access` are protected by the `Admin` authorization policy. User access state is stored in `User.IsActive`, login activity in `User.LastLoginAt`, and the access handler prevents self-demotion, self-lockout, and removal of the last active administrator. JWT validation rechecks active state and role so lockouts and role changes take effect on the next request.
- Admin navbar feature controls: `/admin/settings` can show or hide each user feature link in the shared navbar. `GET /api/settings/navigation/features` returns the authenticated-user configuration with enabled defaults, while existing admin-only setting updates persist each `NAV_*` toggle in `SystemSettings`. These controls affect navigation visibility only; routes and API authorization remain unchanged.
- Admin Market Assets: `/admin/market-assets` supports server-side search by symbol/name/external ID, category/source/status filters, sortable columns, and paginated results. The default order is symbol ascending (alphabetical), with deterministic ID tie-breaking.
- Telegram expense capture: `/chi [amount] "[category]" "[description]" [yyyy-MM-dd|dd/MM/yyyy]` records an expense for the earliest-created active Admin and that Admin's earliest-created portfolio. A successful command atomically creates the linked `CashflowRecord`, fiat withdrawal `Transaction`, and cash-ledger entry. `/cf` remains available for backward-compatible income/expense capture.
- Transaction tracking UX: `/transactions` now groups the global ledger into Crypto, Cổ phiếu, and CCQ/ETF tabs (with Vietnamese/English category matching), quick counts, category-aware add-transaction entry point, and an Edit action that reuses the existing user-scoped update endpoint. The add modal shows loading feedback while assets/categories are being resolved, a ledger-aligned payment/receipt total including fees, and a custom themed date-time picker. The existing user-scoped transaction API contract remains unchanged.
- Transaction amount entry: the global Add Transaction modal accepts any two of quantity, unit price, and gross transaction total, then derives the third value. Users explicitly choose VND or USD; create/update handlers apply that requested currency to the linked cash-ledger account, while requests without a currency retain the asset-category fallback.
- Transaction file transfer: the `/transactions` ledger has a shared All/Crypto/Stock/Fund scope for import and export. It supports CSV, SpreadsheetML/binary XLS, and paginated PDF; PDF import understands official OKX and Binance Spot trading-history layouts. Binance fills preserve UTC+7 timestamps and normalize base-, quote-, or BNB-denominated fees into the transaction quote currency using the nearest BNB quote found in the report. Import opens a review step before any write, classifies ready/duplicate/invalid/out-of-scope rows, exposes missing Market Assets and portfolio assets, and lets Admins create and attach missing symbols inline. Duplicate fingerprints are skipped, assets and portfolios are resolved by ID/name, and writes continue through the existing validated transaction command.
- Transaction PDF export: generated reports use a landscape dashboard layout with scope/date metadata, KPI cards, readable transaction columns, type badges, page numbering, and embedded CorePortfolio data for lossless re-import.
- Transaction bulk deletion: authenticated users can delete all transactions or one asset group through `DELETE /api/transactions?assetGroup=All|Crypto|Stock|Fund`. The operation is user-scoped and atomic, removes linked cash-ledger entries and transaction-generated cashflows, and preserves portfolios, assets, and cash accounts.
- Transaction accounting and counts: Crypto sales whose acquisition history is missing are excluded from realized PnL until a tracked cost basis exists, preventing partial exchange imports from reporting sale proceeds as pure profit. Transaction group cards aggregate every API page (while respecting active type/date filters), rather than counting only the visible page.
- Portfolio valuation: `CurrentTotalValue` is the VND-converted market value of investment holdings only. Cash remains separately available through `CashBalances`; net-worth consumers add holdings and cash exactly once.
- Portfolio allocation report: `/portfolios/:id` shows VND-normalized holding percentages for Crypto, CCQ/ETF, and Stock, with Vietnamese/English category matching. Percentages use investment holdings as the denominator and exclude cash.

- Crypto rewards are recorded with `TransactionType.Earn`: they increase holding quantity at zero acquisition value (apart from an optional capitalized fee), create no purchase cash flow, and are rejected for non-crypto categories. Crypto imports tolerate sales above the tracked quantity, but exclude the unknown-basis portion from realized PnL; Stock and CCQ/ETF retain strict oversell validation. Accounting uses weighted-average cost, applies acquisitions before sales at the same timestamp, and reports realized, unrealized, and combined PnL separately. Portfolio category headers show combined PnL percentages for Stock, CCQ/ETF, Crypto, and any configured category/currency group; performance analytics includes realized PnL and closed positions.

## Verification commands

```powershell
dotnet build backend/src/CorePortfolio.API/CorePortfolio.API.csproj
dotnet test backend/src/CorePortfolio.Domain.Tests/CorePortfolio.Domain.Tests.csproj
# API integration tests are intentionally omitted from the default agent loop because they are slow.
# Run them only when explicitly requested or when changing auth/persistence boundaries.
npm run check:encoding
cd frontend
npm run build
```

## CI/CD

- `.github/workflows/backend-ci.yml` restores and builds the API project directly, runs domain and API integration tests, and verifies the EF snapshot.
- `.github/workflows/frontend-ci.yml` runs blocking `npm ci`, Vitest, and the production build; ESLint runs as an advisory step while legacy lint violations are migrated.
- `.github/workflows/text-encoding-ci.yml` validates source files as strict UTF-8 and rejects common mojibake markers across backend, frontend, documentation, workflows, and agent instructions.
- `.github/workflows/main_coreportfolio-api.yml` restores the API with the `linux-x64` runtime target, builds it, runs domain and API integration tests, publishes a self-contained Linux artifact, transfers it as a tar archive to preserve executable permissions, checks `/health/live` as a blocking smoke test, and logs `/health/ready` database status.
- Production deployment requires the Azure publish-profile secret and the API App Service CORS configuration described above. Vercel frontend deployment remains managed by Vercel; frontend CI is the merge gate.

If an environment prevents backend restore/build, report the exact limitation; do not claim the backend is verified.

## Update policy

When a task changes routes, endpoints, entities, migrations, or feature status, update this document before final handoff. Keep entries concise and link to source paths when adding detailed contracts.
