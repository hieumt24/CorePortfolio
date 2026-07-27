export function getAdminSection(pathname: string): string | null {
  const segments = pathname.split('/').filter(Boolean);
  const adminIndex = segments.indexOf('admin');

  if (adminIndex === -1) return null;
  return segments[adminIndex + 1] ?? null;
}
