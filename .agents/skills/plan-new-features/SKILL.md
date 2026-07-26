---
name: plan-new-features
description: Analyze the current CorePortfolio source and produce an implementation-ready feature plan with priorities, dependencies, backend slices, schema, frontend UX, authorization, tests, migration, production impact, and definition of done. Use when the user asks for ideas, TODO refinement, roadmap, sprint planning, feature decomposition, or what to implement next.
---

# Plan new CorePortfolio features

Produce a source-grounded plan in the conversation. Do not modify code or create a planning document unless explicitly requested.

## 1. Read current reality

1. Read `.agents/AGENTS.md` and `docs/PROJECT_CONTEXT.md` completely.
2. Inspect relevant backend features, entities, EF configuration, endpoints, frontend routes/components/API clients, and production configuration.
3. Check `git status --short` to distinguish committed functionality from unfinished work.
4. Search for the capability before proposing it. Classify each item as already implemented, partially implemented, missing, or blocked.

Do not rely only on prior chat summaries when the repository can confirm the state.

## 2. Build the dependency map

For each proposal identify:

- User/admin outcome and existing foundation to reuse.
- Domain invariant and ownership boundary.
- Schema, indexes, migration, and backfill.
- MediatR commands/queries and Minimal API routes.
- Authorization and permission requirements.
- Frontend route, components, states, and responsive behavior.
- Jobs, idempotency, dedupe, concurrency, and audit needs.
- Production configuration, provider, privacy, backup, and rollback impact.
- Unit, integration, migration, and manual smoke-test coverage.

Do not schedule UI before its contract or financial calculations before data integrity.

## 3. Prioritize

- `P0`: security, correctness, broken production, data loss, or prerequisite foundation.
- `P1`: high user value on stable foundations.
- `P2`: operational efficiency and broader workflows.
- `P3`: optimization, polish, or optional expansion.

Prefer thin end-to-end slices. Make every sprint independently verifiable and deployable.

## 4. Required output

Respond in Vietnamese with concise bullets:

1. Current-state findings.
2. Recommended order and dependency rationale.
3. For each sprint: goal, backend, database/migration, frontend, security/operations, tests, and Definition of Done.
4. Out-of-scope items and deferred dependencies.
5. The first concrete task to implement.

Suggest folder/API names only when useful. Do not fabricate unstable provider contracts, requirements, or data.

## CorePortfolio constraints

- Use Vertical Slice Architecture, MediatR, and Minimal APIs.
- Always scope user-owned data to the authenticated user.
- Persist UTC timestamps; display timestamps in `Asia/Ho_Chi_Minh`; preserve date-only fields.
- Support Vietnamese and English category matching.
- KBS prices are keyless absolute VND values; failures preserve stale data.
- Sensitive admin actions require least privilege, audit, confirmation, and rollback.
- Validate SQLite migrations from an empty database and consider existing production data.

## Avoid

- Re-proposing completed features.
- Mixing unrelated domains into one migration or sprint.
- Treating navigation visibility as authorization.
- Returning zero when financial data is unavailable.
- Hard-coding unstable market universes or provider contracts.
- Giving unsupported estimates.
- Writing aspirational roadmaps without acceptance criteria.
