import { describe, expect, it } from 'vitest';
import { getAdminSection } from './adminRoute';

describe('getAdminSection', () => {
  it('returns the top-level admin section', () => {
    expect(getAdminSection('/admin/users')).toBe('users');
  });

  it('keeps a nested user detail route in the users section', () => {
    expect(getAdminSection('/admin/users/7f31c1e8-4d3e-44a5-9e11-b2b924216d93')).toBe('users');
  });

  it('returns null when the admin section is absent', () => {
    expect(getAdminSection('/admin')).toBeNull();
    expect(getAdminSection('/dashboard')).toBeNull();
  });
});
