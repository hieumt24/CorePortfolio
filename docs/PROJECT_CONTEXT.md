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

## Verification commands

```powershell
dotnet build backend/src/CorePortfolio.API/CorePortfolio.API.csproj
dotnet test backend/src/CorePortfolio.Domain.Tests/CorePortfolio.Domain.Tests.csproj
cd frontend
npm run build
```

If an environment prevents backend restore/build, report the exact limitation; do not claim the backend is verified.

## Update policy

When a task changes routes, endpoints, entities, migrations, or feature status, update this document before final handoff. Keep entries concise and link to source paths when adding detailed contracts.
