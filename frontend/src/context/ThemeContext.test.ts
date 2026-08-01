import { describe, expect, it } from 'vitest';
import { getInitialTheme, isTheme } from './ThemeContext';

describe('theme preference', () => {
  it('accepts only supported stored themes', () => {
    expect(isTheme('light')).toBe(true);
    expect(isTheme('dark')).toBe(true);
    expect(isTheme('system')).toBe(false);
  });

  it('prefers a valid stored choice over the operating system', () => {
    expect(getInitialTheme('light', true)).toBe('light');
    expect(getInitialTheme('dark', false)).toBe('dark');
  });

  it('uses the operating-system preference when no valid choice exists', () => {
    expect(getInitialTheme(null, true)).toBe('dark');
    expect(getInitialTheme('invalid', false)).toBe('light');
  });
});
