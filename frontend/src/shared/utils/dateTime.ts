const VIETNAM_TIME_ZONE = 'Asia/Ho_Chi_Minh';
const DATE_ONLY_PATTERN = /^\d{4}-\d{2}-\d{2}$/;
const EXPLICIT_TIME_ZONE_PATTERN = /(?:Z|[+-]\d{2}:\d{2})$/i;

export const parseApiDateTime = (value: string | Date): Date => {
  if (value instanceof Date) return value;
  const normalized = value.trim();
  if (DATE_ONLY_PATTERN.test(normalized))
    return new Date(`${normalized}T00:00:00+07:00`);
  if (EXPLICIT_TIME_ZONE_PATTERN.test(normalized))
    return new Date(normalized);
  return new Date(`${normalized}Z`);
};

export const formatVietnamDateTime = (
  value: string | Date | null | undefined,
  fallback = '—',
) => {
  if (!value) return fallback;
  const date = parseApiDateTime(value);
  if (Number.isNaN(date.getTime())) return fallback;
  return new Intl.DateTimeFormat('vi-VN', {
    timeZone: VIETNAM_TIME_ZONE,
    dateStyle: 'short',
    timeStyle: 'short',
    hour12: false,
  }).format(date);
};

export const formatVietnamDate = (
  value: string | Date | null | undefined,
  fallback = '—',
  options: Intl.DateTimeFormatOptions = {},
) => {
  if (!value) return fallback;
  const date = parseApiDateTime(value);
  if (Number.isNaN(date.getTime())) return fallback;
  return new Intl.DateTimeFormat('vi-VN', {
    timeZone: VIETNAM_TIME_ZONE,
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    ...options,
  }).format(date);
};

export const formatVietnamTime = (
  value: string | Date | null | undefined,
  fallback = '—',
) => {
  if (!value) return fallback;
  const date = parseApiDateTime(value);
  if (Number.isNaN(date.getTime())) return fallback;
  return new Intl.DateTimeFormat('vi-VN', {
    timeZone: VIETNAM_TIME_ZONE,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  }).format(date);
};

export const vietnamTodayIso = () => {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: VIETNAM_TIME_ZONE,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date());
  const get = (type: Intl.DateTimeFormatPartTypes) =>
    parts.find((part) => part.type === type)?.value ?? '';
  return `${get('year')}-${get('month')}-${get('day')}`;
};

export { VIETNAM_TIME_ZONE };
