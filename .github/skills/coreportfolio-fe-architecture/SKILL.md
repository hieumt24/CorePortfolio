---
name: coreportfolio-fe-architecture
description: Guidelines for CorePortfolio Frontend architecture and folder structure using React + Vite. Use this skill when setting up the project, creating new feature modules, or deciding where to place files.
license: MIT
metadata:
  author: CorePortfolio
  version: "1.0.0"
---

# CorePortfolio Frontend Architecture

This document defines the architectural conventions for the CorePortfolio Frontend application built with **React, Vite, TypeScript, and Vanilla CSS**.

## Architectural Pattern: Vertical Slice / Feature-Sliced Design

Similar to the Backend API, the Frontend heavily relies on a domain-driven approach. We organize code by **Features** rather than by technical roles (e.g., no global `components` or `hooks` folders unless they are truly shared across the entire app).

### Folder Structure

```
src/
├── app/                  # Application layer: Global styles (index.css), Providers, App.tsx, main.tsx
├── features/             # Feature slices (Vertical Slices)
│   ├── portfolios/       # Domain: Portfolios
│   │   ├── api/          # API calls related to portfolios
│   │   ├── components/   # UI components specific to portfolios
│   │   ├── hooks/        # Custom hooks specific to portfolios
│   │   └── types/        # TypeScript interfaces/types
│   ├── assets/           # Domain: Assets
│   └── transactions/     # Domain: Transactions
├── shared/               # Shared cross-domain code
│   ├── api/              # Base API client (fetch/axios instances)
│   ├── ui/               # Generic UI components (Button, Input, Card)
│   ├── utils/            # Helper functions
│   └── hooks/            # Generic hooks (useDebounce, useTheme)
└── config/               # Environment variables, constants
```

## Rules & Conventions

1. **Feature Isolation**: 
   - A feature should not depend on another feature's internal components.
   - If two features need the same component, it should be moved to `shared/ui`.

2. **API Layer**:
   - Keep API call logic in the `api/` folder of each feature. 
   - Use the base client from `shared/api/baseClient.ts` to make requests to `http://localhost:5211`.

3. **Routing**:
   - Routes can be defined in `app/routes` or directly inside `App.tsx` (for simplicity).
   - Route components should just import the main feature component (e.g., `<PortfolioDashboard />`).

4. **Styling**:
   - Use **Vanilla CSS**.
   - Create isolated CSS files next to the component (e.g., `PortfolioList.tsx` and `PortfolioList.css`).
   - Use CSS variables (`var(--primary-color)`) defined in `app/index.css` for consistent theming.

## When to use this skill
- When creating a new module.
- When scaffolding the base React + Vite project.
- When reviewing pull requests for architectural violations.
