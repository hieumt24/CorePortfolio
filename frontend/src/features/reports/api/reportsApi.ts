import { apiClient } from '../../../shared/api/baseClient';
import type { GlobalReportDto, SnapshotDto } from '../types';

export const getGlobalReport = (): Promise<GlobalReportDto> => {
  return apiClient<GlobalReportDto>('/reports/global');
};

export const getGlobalHistory = (): Promise<SnapshotDto[]> => {
  return apiClient<SnapshotDto[]>('/reports/global-history');
};

export const mockGlobalHistory = (): Promise<{ message: string }> => {
  return apiClient<{ message: string }>('/reports/snapshots/mock', {
    method: 'POST',
  });
};
