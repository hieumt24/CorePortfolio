---
name: fullstack-project-agent
description: Full-stack implementation workflow for CorePortfolio coordinating backend, frontend, documentation, and verification. Use for new features, cross-layer bugs, and UI redesigns.
---

# Fullstack Project Agent

Use this skill for every implementation task in CorePortfolio. Treat backend, frontend, documentation, and verification as one deliverable.

## Required workflow

1. Read `.agents/AGENTS.md` and `docs/PROJECT_CONTEXT.md` before changing code. If the context document is missing or stale, regenerate it from the source tree before implementation.
2. Inspect both `backend/` and `frontend/`, even for UI-only or API-only requests. Identify the domain entity, endpoint/query, frontend API client/types, route, and component affected.
3. Implement backend changes using vertical slices, MediatR, Minimal APIs, and `ICurrentUserService`. Add EF configuration and migration for schema changes.
4. Implement the frontend contract in the same task: API client, types, loading/error/empty states, route or navigation entry, and responsive styling. Use existing design tokens; read the premium UI skill for visual redesigns.
5. Update `docs/PROJECT_CONTEXT.md` whenever architecture, routes, API contracts, entities, migrations, or feature status changes.
6. Verify with `git diff --check`, backend build/test, and `npm run build` from `frontend`. Report warnings separately from failures.

## Cross-layer checklist

- Request/response DTOs match TypeScript types.
- Every user-owned query filters by current user.
- Dates use UTC; currency conversion is explicit; recurring operations are idempotent.
- Loading, error, empty, success, and retry states exist.
- New user-facing functionality is reachable from a route or Navbar.
- Documentation is updated in the same change.

## Guardrails

- Do not implement only one stack without documenting why the other is unaffected.
- Do not add MVC controllers when a feature slice and Minimal API fit the project.
- Do not hide backend build failures behind a successful frontend-only check.
