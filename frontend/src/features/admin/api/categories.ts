import { apiClient } from '../../../shared/api/baseClient';
import type { AssetCategory, CreateCategoryRequest } from '../types';

export const categoriesApi = {
  getCategories: () => 
    apiClient<AssetCategory[]>('/admin/categories', { method: 'GET' }),
    
  createCategory: (data: CreateCategoryRequest) =>
    apiClient<{ id: string }>('/admin/categories', { method: 'POST', body: JSON.stringify(data) }),

  updateCategory: (id: string, data: CreateCategoryRequest) =>
    apiClient<void>(`/admin/categories/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  deleteCategory: (id: string) =>
    apiClient<void>(`/admin/categories/${id}`, { method: 'DELETE' }),
};
