---
name: write-fe-api-client
description: Skill for creating or updating frontend API fetchers and hooks to connect to the MediatR Backend (http://localhost:5211).
license: MIT
metadata:
  author: CorePortfolio
  version: "1.0.0"
---

# Write FE API Client (CorePortfolio)

Use this skill to fetch data from the Backend API.

## Step 1: Base Client Configuration
All requests should go through a base fetch wrapper or Axios instance located at `src/shared/api/baseClient.ts`.
By default, the backend runs on `http://localhost:5211`.

```typescript
// src/shared/api/baseClient.ts
const API_URL = 'http://localhost:5211/api';

export const apiClient = async <T>(endpoint: string, options?: RequestInit): Promise<T> => {
  const response = await fetch(`${API_URL}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`API Error: ${response.statusText}`);
  }

  // Handle 204 No Content
  if (response.status === 204) {
    return {} as T;
  }

  return response.json();
};
```

## Step 2: Define Feature Specific Types and Fetchers
In `src/features/<feature>/api/<feature>Api.ts`, write the specific endpoints.

```typescript
// src/features/portfolios/api/portfolioApi.ts
import { apiClient } from '../../../shared/api/baseClient';
import { PortfolioSummaryDto } from '../types';

export const getPortfolioSummary = (portfolioId: string): Promise<PortfolioSummaryDto> => {
  return apiClient<PortfolioSummaryDto>(`/portfolios/${portfolioId}/summary`);
};
```

## Step 3: Implement Custom Hooks
Wrap the fetchers in custom hooks (`src/features/<feature>/hooks/`) to handle loading, error, and data states elegantly. You may use `SWR` or `@tanstack/react-query` if installed, otherwise implement a basic `useEffect` hook.

```typescript
// src/features/portfolios/hooks/usePortfolioSummary.ts
import { useState, useEffect } from 'react';
import { getPortfolioSummary } from '../api/portfolioApi';
import { PortfolioSummaryDto } from '../types';

export const usePortfolioSummary = (portfolioId: string) => {
  const [data, setData] = useState<PortfolioSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    let isMounted = true;
    
    getPortfolioSummary(portfolioId)
      .then(res => {
        if (isMounted) setData(res);
      })
      .catch(err => {
        if (isMounted) setError(err);
      })
      .finally(() => {
        if (isMounted) setLoading(false);
      });

    return () => { isMounted = false; };
  }, [portfolioId]);

  return { data, loading, error };
};
```

## Error Handling
Always ensure components using these hooks display graceful loading skeletons and meaningful error messages rather than completely breaking.
