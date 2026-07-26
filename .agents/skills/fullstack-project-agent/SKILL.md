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
6. Verify with `git diff --check`, an explicit backend project build, relevant fast unit/domain tests, and `npm run build` from `frontend`. Skip API integration tests by default because they are slow; run them only when the user explicitly requests them. Report skipped integration coverage, warnings, and failures separately.
7. For changes involving UI, stop after automated verification and provide the user with a short, task-specific manual smoke-test guide. Do not start local servers, control a browser, perform visual smoke testing, or wait for UI confirmation unless the user explicitly asks for it. The user may run the smoke test and attach screenshots/files for a follow-up review.

## Cross-layer checklist

- Request/response DTOs match TypeScript types.
- Every user-owned query filters by current user.
- Dates use UTC; currency conversion is explicit; recurring operations are idempotent.
- Loading, error, empty, success, and retry states exist.
- New user-facing functionality is reachable from a route or Navbar.
- Documentation is updated in the same change.
- UI handoff identifies the route, exact interactions to try, expected result, and the most useful screenshot/error details to attach if something fails.

## Guardrails

- Do not implement only one stack without documenting why the other is unaffected.
- Do not add MVC controllers when a feature slice and Minimal API fit the project.
- Do not hide backend build failures behind a successful frontend-only check.
- Do not claim integration coverage was verified when integration tests were skipped.
- Do not make a requested commit or push wait on manual UI smoke testing unless the user explicitly makes that smoke test a completion gate.
