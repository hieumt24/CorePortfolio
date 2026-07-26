---
name: fullstack-project-agent
description: Implement, debug, harden, or redesign CorePortfolio features across ASP.NET Core, EF Core, React, production configuration, tests, documentation, and Git handoff. Use for code changes that affect backend, frontend, database, authentication, deployment, or cross-layer contracts.
---

# Full-stack CorePortfolio workflow

Treat code, database, UI, production configuration, verification, and documentation as one deliverable.

## 1. Establish the baseline

1. Read `.agents/AGENTS.md` and `docs/PROJECT_CONTEXT.md` completely.
2. Inspect `git status --short`, the current branch, and the latest commit.
3. Preserve unrelated user changes. Never reset, overwrite, or stage them without confirming they belong to the requested scope.
4. Trace the complete contract: domain invariant, EF/migration, MediatR slice, Minimal API, TypeScript/API client, route/UI, and production behavior.
5. For bugs, identify the exact failed boundary before editing.

## 2. Design before editing

- Define the smallest end-to-end slice that reaches the user.
- List schema, API, UI, authorization, migration/backfill, and operational impacts.
- Separate account access, presence, permissions, and business ownership.
- Scope user-owned data through `ICurrentUserService`.
- Persist UTC timestamps; use shared Vietnam-time helpers only for display; keep date-only fields timezone-neutral.
- Follow `.agents/AGENTS.md` for market data: KBS is keyless, prices are absolute VND, and failures preserve stale values.

## 3. Implement safely

### Backend

- Use Vertical Slice Architecture under `CorePortfolio.API/Features`.
- Put business logic in MediatR handlers; endpoints only bind and map results.
- Use Minimal APIs, never MVC controllers.
- Enforce authorization at endpoints and sensitive invariants again in handlers.
- Make jobs, imports, broadcasts, migrations, and repairs idempotent.
- Audit sensitive mutations without passwords, tokens, secrets, or excessive personal data.
- Use optimistic concurrency where concurrent edits are plausible.

### Database

- Configure entities, relationships, constraints, and indexes explicitly.
- Generate one focused EF migration per coherent schema change.
- Backfill conservatively; mark unknown data instead of guessing.
- Keep the design-time factory independent from JWT and external providers.

### Frontend

- Keep DTO and TypeScript contracts aligned.
- Provide loading, error, empty, success, retry, disabled, and permission-denied states.
- Make functionality reachable by route and appropriate navigation.
- Use shared API/date utilities and existing design tokens.
- For redesigns, read `premium-glassmorphism-ui`; read `hallmark` only for explicit audit/redesign requests.
- Preserve keyboard focus, accessibility, 44px touch targets, and responsive behavior.

### Production

- Verify `/api` routing precedes SPA fallback.
- Never expose secrets, server paths, connection strings, or provider keys to frontend code.
- Consider reverse proxies, forwarded IPs, persistent SQLite, readiness, maintenance mode, backup, and rollback.

## 4. Verify proportionally

Always run:

- `git diff --check`
- Explicit Release build of `CorePortfolio.API.csproj`
- Relevant fast unit/domain tests
- `npm test` when frontend logic changed
- `npm run build` from `frontend`
- `npm run check:encoding` when source text changed

For EF changes, also apply all migrations to a new temporary SQLite database and run `dotnet ef migrations has-pending-model-changes`.

API integration tests are opt-in. Run them only when the user explicitly requests them. For authentication, authorization, user isolation, accounting atomicity, migrations, restore, or other high-risk work, recommend integration coverage and clearly report whether it was skipped.

If Vite/Vitest fails with `spawn EPERM` inside the sandbox, rerun the same approved command with required escalation; do not report it as a code failure.

## 5. Review and hand off

1. Review the final diff for scope, generated files, secrets, and build artifacts.
2. Update `docs/PROJECT_CONTEXT.md` for changed entities, migrations, routes, contracts, production settings, or feature status.
3. When commit/push is requested, fetch and compare remote, stage only approved scope, run cached diff check, then confirm remote SHA and a clean worktree.
4. Lead the final response with the outcome, followed by implemented features, verification, warnings, deployment notes, and a short UI smoke-test checklist.

## Guardrails

- Never claim tests, migrations, deployment, commit, or push succeeded without confirmation.
- Never let a frontend success hide a backend failure.
- Do not expand diagnosis into implementation without authorization.
- Do not wait for manual UI testing unless the user made it a completion gate.
- Reuse existing services and slice patterns before adding abstractions.
