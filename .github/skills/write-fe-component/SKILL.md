---
name: write-fe-component
description: Skill for creating or updating a frontend UI component in CorePortfolio. Triggers when asked to write a UI component, screen, or dashboard.
license: MIT
metadata:
  author: CorePortfolio
  version: "1.0.0"
---

# Write FE Component (CorePortfolio)

Use this skill when you need to write or scaffold a new React Component for the Frontend.

## Step 1: Identify Component Location
- Is it domain-specific? Put it in `src/features/<domain_name>/components/`.
- Is it generic (like Button, Modal, Card)? Put it in `src/shared/ui/`.

## Step 2: Create Files
For a component named `AssetCard`, create two files:
1. `AssetCard.tsx`
2. `AssetCard.css`

## Step 3: Implement the Component (TSX)
- Use standard functional components.
- Import the CSS file directly: `import './AssetCard.css';`
- Assign a unique and descriptive `className` to the root element matching the component name (e.g., `className="asset-card"`).

```tsx
// Example: AssetCard.tsx
import React from 'react';
import './AssetCard.css';

interface AssetCardProps {
  name: string;
  symbol: string;
  currentPrice: number;
}

export const AssetCard: React.FC<AssetCardProps> = ({ name, symbol, currentPrice }) => {
  return (
    <div className="asset-card">
      <div className="asset-card-header">
        <h3 className="asset-name">{name}</h3>
        <span className="asset-symbol">{symbol}</span>
      </div>
      <div className="asset-card-body">
        <span className="asset-price">${currentPrice.toFixed(2)}</span>
      </div>
    </div>
  );
};
```

## Step 4: Implement Styles (CSS)
- Use Vanilla CSS.
- Apply rich aesthetics: Glassmorphism, smooth transitions, modern typography, and hover effects.
- Ensure the component looks premium.

```css
/* Example: AssetCard.css */
.asset-card {
  background: rgba(25, 25, 30, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 16px;
  padding: 20px;
  transition: transform 0.3s ease, box-shadow 0.3s ease;
  backdrop-filter: blur(12px);
  color: white;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.asset-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
  border-color: rgba(255, 255, 255, 0.2);
}

.asset-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.asset-name {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 600;
}

.asset-symbol {
  font-size: 0.9rem;
  opacity: 0.7;
  background: rgba(255, 255, 255, 0.1);
  padding: 4px 8px;
  border-radius: 6px;
}

.asset-price {
  font-size: 1.5rem;
  font-weight: 700;
  color: #4ade80; /* Modern neon green */
}
```

## When NOT to use this skill
- When updating API fetching logic (use `write-fe-api-client`).
