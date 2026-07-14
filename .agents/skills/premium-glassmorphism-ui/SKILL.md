---
name: premium-glassmorphism-ui
description: Premium vibrant glassmorphism design principles for CorePortfolio frontend, focusing on modern aesthetics, dynamic gradients, deep glowing shadows, and micro-animations. Use this skill when asked to design or redesign frontend UI components to ensure they feel rich, engaging, and premium.
---

# Premium Glassmorphism UI Guidelines

This skill defines the primary aesthetic for the CorePortfolio frontend application. It replaces strict, dry, industrial "fintech" designs with a rich, dynamic, and visually stunning user experience while maintaining professionalism.

## 1. Core Principles
- **Aesthetic**: Premium, Modern, Vibrant, Glassmorphic.
- **Goal**: Create a "WOW" first impression through depth, color, and smooth interaction.
- **Vibe**: A high-end consumer finance app (think Apple Card, modern neobanks) rather than a raw trading terminal.

## 2. Color Palette & Lighting
- **Dark Mode Base**: The background should not be pitch black. Use a deep, rich indigo/navy base (e.g., `#06080F` to `#0B0E17`).
- **Gradients over Solids**: Use gradients for primary actions, titles, and active states.
  - Primary Gradient: `linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%)` (Indigo to Violet).
  - Text Gradients: Use background-clip text gradients for main headers to make them pop.
- **Glowing Elements**: Add subtle, colored box-shadows to primary buttons and active cards to simulate glowing (`box-shadow: 0 4px 15px rgba(99, 102, 241, 0.4)`).

## 3. Glassmorphism & Depth
- **Translucency**: Panels and cards MUST use `backdrop-filter: blur(16px)` with semi-transparent backgrounds (e.g., `rgba(15, 23, 42, 0.6)`).
- **Glass Borders**: Elements need a subtle, high-opacity thin border to catch the light (e.g., `border: 1px solid rgba(255, 255, 255, 0.08)`).
- **Layering**: Use shadows to create physical depth (`box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3)`).

## 4. Typography & Geometry
- **Geometry**: Soften the edges. Use `border-radius: 16px` or `20px` for main panels, and `8px` or `12px` for smaller elements like buttons and inputs.
- **Typography**: Keep it highly legible but stylish. Avoid overly strict "tabular" configurations for everything unless it's a dense ledger.

## 5. Micro-animations (8-State Discipline)
All interactive elements MUST feel alive:
- **Hover**: Scale up slightly (`transform: translateY(-4px) scale(1.02)`) and intensify glows/shadows.
- **Focus**: Clean, sharp offset outlines for accessibility.
- **Active**: Scale down (`transform: translateY(0) scale(0.98)`) for tactile feedback.
- **Transitions**: Use smooth cubic-bezier transitions (`transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1)`).

## 6. Implementation Rules
- Prefer using the `.glass-panel` utility class for all container surfaces.
- Avoid raw CSS variables like `hm-surface`; name variables semantically around the visual effect (e.g., `--glass-bg`, `--glass-border`, `--glass-glow`).
- For empty states or loading states, ensure the UI remains engaging (e.g., spinning gradient loaders, translucent empty state cards).
