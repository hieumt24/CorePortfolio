---
name: coreportfolio-react-best-practices
description: React and UI Best Practices for CorePortfolio. Follow this when writing React code, applying styles, or reviewing PRs.
license: MIT
metadata:
  author: CorePortfolio
  version: "1.0.0"
---

# React & UI Best Practices (CorePortfolio)

This document contains rules for writing React components and applying Vanilla CSS to achieve Rich Aesthetics.

## 1. React & TypeScript Guidelines

### Functional Components
- Always use Functional Components with React Hooks.
- Define prop types using `type` or `interface` clearly.
- Export components as default or named exports consistently (prefer named exports for feature components, default exports for pages/routes).

### Clean Code & Hooks
- Extract complex logic into custom hooks (e.g., `usePortfolios`, `useAssetPrice`).
- Keep components small and focused on rendering UI.
- Use `useMemo` and `useCallback` when passing props to heavily rendered children, but avoid premature optimization.

## 2. Styling & Rich Aesthetics (CRITICAL)

The application MUST look premium, dynamic, and state-of-the-art.

### Vanilla CSS over Frameworks
- **Do NOT use Tailwind CSS**, Bootstrap, or Material UI unless explicitly requested by the user.
- Use **Vanilla CSS** (`.css` files) to ensure full control over animations and design.

### Design Principles
1. **Modern Typography**: Use clean fonts like Inter, Roboto, or Outfit. Define them in `app/index.css`.
2. **Glassmorphism**: Utilize background blur, semi-transparent backgrounds, and subtle borders to create depth.
   ```css
   .glass-card {
     background: rgba(255, 255, 255, 0.05);
     backdrop-filter: blur(10px);
     border: 1px solid rgba(255, 255, 255, 0.1);
     border-radius: 12px;
   }
   ```
3. **Color Palettes & Dark Mode**: Provide a sleek Dark Mode by default. Use HSL or modern HEX colors for primary accents (e.g., Neon Blue, Purple gradients). Avoid generic plain red/blue.
4. **Micro-Animations & Hover Effects**: Buttons and cards must have subtle transitions.
   ```css
   .btn {
     transition: transform 0.2s ease, box-shadow 0.2s ease;
   }
   .btn:hover {
     transform: translateY(-2px);
     box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
   }
   ```

### Avoiding Generic UI
- Do not build simple "Minimum Viable Product" (MVP) tables. Use cards with icons, visual status indicators (e.g., green/red for profit/loss), and skeletons for loading states.

## When to use this skill
- Reviewing React code for styling or structural flaws.
- Designing a new UI component.
- Polishing an existing screen to make it look "premium".
